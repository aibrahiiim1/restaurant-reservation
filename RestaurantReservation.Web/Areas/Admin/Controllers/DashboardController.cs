using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantReservation.Web.Data;
using RestaurantReservation.Web.Models.Entities;
using RestaurantReservation.Web.Models.ViewModels;

namespace RestaurantReservation.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin,RestaurantManager,BranchManager")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _context.Users
            .OfType<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound();
        }

        // Check user role and filter data accordingly
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var isRestaurantManager = User.IsInRole("RestaurantManager");
        var isBranchManager = User.IsInRole("BranchManager");

        IQueryable<Branch> branchQuery = _context.Branches.Include(b => b.Restaurant);
        IQueryable<Booking> bookingQuery = _context.Bookings.Include(b => b.Table);
        IQueryable<Review> reviewQuery = _context.Reviews;

        if (isRestaurantManager && user.RestaurantId.HasValue)
        {
            branchQuery = branchQuery.Where(b => b.RestaurantId == user.RestaurantId);
            var branchIds = await branchQuery.Select(b => b.Id).ToListAsync();
            bookingQuery = bookingQuery.Where(b => branchIds.Contains(b.BranchId));
            reviewQuery = reviewQuery.Where(r => branchIds.Contains(r.BranchId));
        }
        else if (isBranchManager && user.BranchId.HasValue)
        {
            branchQuery = branchQuery.Where(b => b.Id == user.BranchId);
            bookingQuery = bookingQuery.Where(b => b.BranchId == user.BranchId);
            reviewQuery = reviewQuery.Where(r => r.BranchId == user.BranchId);
        }

        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var viewModel = new AdminDashboardViewModel
        {
            TotalRestaurants = isSuperAdmin 
                ? await _context.Restaurants.CountAsync() 
                : (isRestaurantManager ? 1 : 0),
            TotalBranches = await branchQuery.CountAsync(),
            TotalBookings = await bookingQuery.CountAsync(),
            TodayBookings = await bookingQuery.CountAsync(b => b.BookingDate.Date == today),
            PendingBookings = await bookingQuery.CountAsync(b => b.Status == BookingStatus.Pending),
            PendingReviews = await reviewQuery.CountAsync(r => !r.IsApproved),
            TodayRevenue = await bookingQuery
                .Where(b => b.BookingDate.Date == today && b.DepositPaid)
                .SumAsync(b => b.DepositAmount ?? 0),
            MonthlyRevenue = await bookingQuery
                .Where(b => b.BookingDate >= monthStart && b.DepositPaid)
                .SumAsync(b => b.DepositAmount ?? 0),
            RecentBookings = await bookingQuery
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .Select(b => new BookingListViewModel
                {
                    Id = b.Id,
                    BookingReference = b.BookingReference,
                    GuestName = b.GuestName,
                    GuestEmail = b.GuestEmail,
                    PartySize = b.PartySize,
                    BookingDate = b.BookingDate,
                    BookingTime = b.BookingTime,
                    TableNumber = b.Table.TableNumber,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync(),
            RecentReviews = await reviewQuery
                .Include(r => r.Branch)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
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
                .ToListAsync()
        };

        return View(viewModel);
    }
}
