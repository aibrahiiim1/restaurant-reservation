using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantReservation.Web.Data;
using RestaurantReservation.Web.Models.Entities;
using RestaurantReservation.Web.Models.ViewModels;

namespace RestaurantReservation.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin,RestaurantManager,BranchManager")]
public class ReviewsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(ApplicationDbContext context, ILogger<ReviewsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int? branchId, bool? pending)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _context.Users.OfType<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == userId);

        IQueryable<Review> query = _context.Reviews.Include(r => r.Branch);

        if (User.IsInRole("RestaurantManager") && user?.RestaurantId.HasValue == true)
        {
            query = query.Where(r => r.Branch.RestaurantId == user.RestaurantId);
        }
        else if (User.IsInRole("BranchManager") && user?.BranchId.HasValue == true)
        {
            query = query.Where(r => r.BranchId == user.BranchId);
        }
        else if (branchId.HasValue)
        {
            query = query.Where(r => r.BranchId == branchId);
        }

        if (pending == true)
        {
            query = query.Where(r => !r.IsApproved);
        }

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewAdminViewModel
            {
                Id = r.Id,
                BranchName = r.Branch.Name,
                GuestName = r.GuestName,
                Rating = r.Rating,
                Comment = r.Comment,
                IsApproved = r.IsApproved,
                IsVisible = r.IsVisible,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        // Get branches for filter
        IQueryable<Branch> branchQuery = _context.Branches.Include(b => b.Restaurant);
        if (User.IsInRole("RestaurantManager") && user?.RestaurantId.HasValue == true)
        {
            branchQuery = branchQuery.Where(b => b.RestaurantId == user.RestaurantId);
        }
        else if (User.IsInRole("BranchManager") && user?.BranchId.HasValue == true)
        {
            branchQuery = branchQuery.Where(b => b.Id == user.BranchId);
        }

        ViewBag.Branches = new SelectList(
            await branchQuery.Select(b => new { b.Id, Name = b.Restaurant.Name + " - " + b.Name }).ToListAsync(),
            "Id",
            "Name",
            branchId);

        ViewBag.CurrentBranchId = branchId;
        ViewBag.ShowPending = pending;

        return View(reviews);
    }

    public async Task<IActionResult> Details(int id)
    {
        var review = await _context.Reviews
            .Include(r => r.Branch)
            .ThenInclude(b => b.Restaurant)
            .Include(r => r.Booking)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review == null)
        {
            return NotFound();
        }

        return View(review);
    }

    public async Task<IActionResult> Moderate(int id)
    {
        var review = await _context.Reviews
            .Include(r => r.Branch)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review == null)
        {
            return NotFound();
        }

        var viewModel = new ReviewModerationViewModel
        {
            Id = review.Id,
            IsApproved = review.IsApproved,
            IsVisible = review.IsVisible,
            AdminResponse = review.AdminResponse
        };

        ViewBag.Review = review;

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Moderate(ReviewModerationViewModel model)
    {
        var review = await _context.Reviews.FindAsync(model.Id);
        if (review == null)
        {
            return NotFound();
        }

        review.IsApproved = model.IsApproved;
        review.IsVisible = model.IsVisible;
        review.AdminResponse = model.AdminResponse;
        review.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(model.AdminResponse) && review.AdminResponseAt == null)
        {
            review.AdminResponseAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Review moderated: {ReviewId}", review.Id);
        TempData["Success"] = "Review updated successfully";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
        {
            return NotFound();
        }

        review.IsApproved = true;
        review.IsVisible = true;
        review.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Review approved successfully";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
        {
            return NotFound();
        }

        review.IsApproved = false;
        review.IsVisible = false;
        review.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Review rejected successfully";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
        {
            return NotFound();
        }

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Review deleted: {ReviewId}", id);
        TempData["Success"] = "Review deleted successfully";

        return RedirectToAction(nameof(Index));
    }
}
