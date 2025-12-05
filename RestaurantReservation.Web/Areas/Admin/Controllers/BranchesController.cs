using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantReservation.Web.Data;
using RestaurantReservation.Web.Models.Entities;
using RestaurantReservation.Web.Models.ViewModels;
using RestaurantReservation.Web.Services.Interfaces;

namespace RestaurantReservation.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin,RestaurantManager,BranchManager")]
public class BranchesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<BranchesController> _logger;

    public BranchesController(
        ApplicationDbContext context,
        IFileUploadService fileUploadService,
        ILogger<BranchesController> logger)
    {
        _context = context;
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int? restaurantId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _context.Users.OfType<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == userId);

        IQueryable<Branch> query = _context.Branches.Include(b => b.Restaurant);

        if (User.IsInRole("RestaurantManager") && user?.RestaurantId.HasValue == true)
        {
            query = query.Where(b => b.RestaurantId == user.RestaurantId);
        }
        else if (User.IsInRole("BranchManager") && user?.BranchId.HasValue == true)
        {
            query = query.Where(b => b.Id == user.BranchId);
        }
        else if (restaurantId.HasValue)
        {
            query = query.Where(b => b.RestaurantId == restaurantId);
        }

        var branches = await query.ToListAsync();

        ViewBag.Restaurants = new SelectList(
            await _context.Restaurants.ToListAsync(), 
            "Id", 
            "Name", 
            restaurantId);

        return View(branches);
    }

    public async Task<IActionResult> Details(int id)
    {
        var branch = await _context.Branches
            .Include(b => b.Restaurant)
            .Include(b => b.Tables)
            .Include(b => b.TimeSlots)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (branch == null)
        {
            return NotFound();
        }

        return View(branch);
    }

    [Authorize(Roles = "SuperAdmin,RestaurantManager")]
    public async Task<IActionResult> Create(int? restaurantId)
    {
        var viewModel = new BranchCreateViewModel
        {
            RestaurantId = restaurantId ?? 0,
            RestaurantList = new SelectList(await _context.Restaurants.ToListAsync(), "Id", "Name", restaurantId)
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RestaurantManager")]
    public async Task<IActionResult> Create(BranchCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.RestaurantList = new SelectList(await _context.Restaurants.ToListAsync(), "Id", "Name", model.RestaurantId);
            return View(model);
        }

        var branch = new Branch
        {
            RestaurantId = model.RestaurantId,
            Name = model.Name,
            Description = model.Description,
            Address = model.Address,
            City = model.City,
            State = model.State,
            ZipCode = model.ZipCode,
            Country = model.Country,
            Latitude = model.Latitude,
            Longitude = model.Longitude,
            Phone = model.Phone,
            Email = model.Email,
            Website = model.Website,
            Cuisine = model.Cuisine,
            Capacity = model.Capacity,
            Area = model.Area,
            HasParking = model.HasParking,
            PaymentOptions = model.PaymentOptions,
            DressCode = model.DressCode,
            IsAccessible = model.IsAccessible,
            IsChildFriendly = model.IsChildFriendly,
            BookingIntervalMinutes = model.BookingIntervalMinutes,
            CancellationPolicyHours = model.CancellationPolicyHours,
            MinimumCharge = model.MinimumCharge,
            RequireDeposit = model.RequireDeposit,
            DepositAmount = model.DepositAmount,
            OperatingHoursJson = model.OperatingHoursJson,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        if (!string.IsNullOrEmpty(model.ClosedDaysInput))
        {
            var closedDays = model.ClosedDaysInput.Split(',').Select(d => d.Trim()).ToList();
            branch.ClosedDaysJson = JsonSerializer.Serialize(closedDays);
        }

        if (model.Logo != null)
        {
            branch.LogoUrl = await _fileUploadService.UploadFileAsync(model.Logo, "branches");
        }

        if (model.Photos != null && model.Photos.Any())
        {
            var photoUrls = await _fileUploadService.UploadFilesAsync(model.Photos, "branches");
            branch.PhotosJson = JsonSerializer.Serialize(photoUrls);
        }

        _context.Branches.Add(branch);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Branch created: {BranchName}", branch.Name);
        TempData["Success"] = "Branch created successfully";

        return RedirectToAction(nameof(Index), new { restaurantId = branch.RestaurantId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var branch = await _context.Branches
            .Include(b => b.Restaurant)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (branch == null)
        {
            return NotFound();
        }

        var photoUrls = new List<string>();
        if (!string.IsNullOrEmpty(branch.PhotosJson))
        {
            try
            {
                photoUrls = JsonSerializer.Deserialize<List<string>>(branch.PhotosJson) ?? new List<string>();
            }
            catch { }
        }

        var viewModel = new BranchEditViewModel
        {
            Id = branch.Id,
            RestaurantId = branch.RestaurantId,
            Name = branch.Name,
            Description = branch.Description,
            Address = branch.Address,
            City = branch.City,
            State = branch.State,
            ZipCode = branch.ZipCode,
            Country = branch.Country,
            Latitude = branch.Latitude,
            Longitude = branch.Longitude,
            Phone = branch.Phone,
            Email = branch.Email,
            Website = branch.Website,
            Cuisine = branch.Cuisine,
            Capacity = branch.Capacity,
            Area = branch.Area,
            HasParking = branch.HasParking,
            PaymentOptions = branch.PaymentOptions,
            DressCode = branch.DressCode,
            IsAccessible = branch.IsAccessible,
            IsChildFriendly = branch.IsChildFriendly,
            BookingIntervalMinutes = branch.BookingIntervalMinutes,
            CancellationPolicyHours = branch.CancellationPolicyHours,
            MinimumCharge = branch.MinimumCharge,
            RequireDeposit = branch.RequireDeposit,
            DepositAmount = branch.DepositAmount,
            OperatingHoursJson = branch.OperatingHoursJson,
            CurrentLogoUrl = branch.LogoUrl,
            CurrentPhotoUrls = photoUrls,
            IsActive = branch.IsActive,
            RestaurantList = new SelectList(await _context.Restaurants.ToListAsync(), "Id", "Name", branch.RestaurantId)
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BranchEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.RestaurantList = new SelectList(await _context.Restaurants.ToListAsync(), "Id", "Name", model.RestaurantId);
            return View(model);
        }

        var branch = await _context.Branches.FindAsync(model.Id);
        if (branch == null)
        {
            return NotFound();
        }

        branch.Name = model.Name;
        branch.Description = model.Description;
        branch.Address = model.Address;
        branch.City = model.City;
        branch.State = model.State;
        branch.ZipCode = model.ZipCode;
        branch.Country = model.Country;
        branch.Latitude = model.Latitude;
        branch.Longitude = model.Longitude;
        branch.Phone = model.Phone;
        branch.Email = model.Email;
        branch.Website = model.Website;
        branch.Cuisine = model.Cuisine;
        branch.Capacity = model.Capacity;
        branch.Area = model.Area;
        branch.HasParking = model.HasParking;
        branch.PaymentOptions = model.PaymentOptions;
        branch.DressCode = model.DressCode;
        branch.IsAccessible = model.IsAccessible;
        branch.IsChildFriendly = model.IsChildFriendly;
        branch.BookingIntervalMinutes = model.BookingIntervalMinutes;
        branch.CancellationPolicyHours = model.CancellationPolicyHours;
        branch.MinimumCharge = model.MinimumCharge;
        branch.RequireDeposit = model.RequireDeposit;
        branch.DepositAmount = model.DepositAmount;
        branch.OperatingHoursJson = model.OperatingHoursJson;
        branch.IsActive = model.IsActive;
        branch.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(model.ClosedDaysInput))
        {
            var closedDays = model.ClosedDaysInput.Split(',').Select(d => d.Trim()).ToList();
            branch.ClosedDaysJson = JsonSerializer.Serialize(closedDays);
        }

        if (model.Logo != null)
        {
            if (!string.IsNullOrEmpty(branch.LogoUrl))
            {
                await _fileUploadService.DeleteFileAsync(branch.LogoUrl);
            }
            branch.LogoUrl = await _fileUploadService.UploadFileAsync(model.Logo, "branches");
        }

        if (model.Photos != null && model.Photos.Any())
        {
            var newPhotoUrls = await _fileUploadService.UploadFilesAsync(model.Photos, "branches");
            var existingPhotos = new List<string>();
            if (!string.IsNullOrEmpty(branch.PhotosJson))
            {
                existingPhotos = JsonSerializer.Deserialize<List<string>>(branch.PhotosJson) ?? new List<string>();
            }
            existingPhotos.AddRange(newPhotoUrls);
            branch.PhotosJson = JsonSerializer.Serialize(existingPhotos);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Branch updated: {BranchName}", branch.Name);
        TempData["Success"] = "Branch updated successfully";

        return RedirectToAction(nameof(Index), new { restaurantId = branch.RestaurantId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RestaurantManager")]
    public async Task<IActionResult> Delete(int id)
    {
        var branch = await _context.Branches
            .Include(b => b.Bookings)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (branch == null)
        {
            return NotFound();
        }

        var activeBookings = branch.Bookings.Any(b => 
            b.Status == BookingStatus.Pending || 
            b.Status == BookingStatus.Confirmed);

        if (activeBookings)
        {
            TempData["Error"] = "Cannot delete branch with active bookings.";
            return RedirectToAction(nameof(Index));
        }

        // Delete associated files
        if (!string.IsNullOrEmpty(branch.LogoUrl))
        {
            await _fileUploadService.DeleteFileAsync(branch.LogoUrl);
        }

        if (!string.IsNullOrEmpty(branch.PhotosJson))
        {
            var photos = JsonSerializer.Deserialize<List<string>>(branch.PhotosJson);
            if (photos != null)
            {
                foreach (var photo in photos)
                {
                    await _fileUploadService.DeleteFileAsync(photo);
                }
            }
        }

        var restaurantId = branch.RestaurantId;
        _context.Branches.Remove(branch);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Branch deleted: {BranchName}", branch.Name);
        TempData["Success"] = "Branch deleted successfully";

        return RedirectToAction(nameof(Index), new { restaurantId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(int branchId, string photoUrl)
    {
        var branch = await _context.Branches.FindAsync(branchId);
        if (branch == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrEmpty(branch.PhotosJson))
        {
            var photos = JsonSerializer.Deserialize<List<string>>(branch.PhotosJson);
            if (photos != null && photos.Contains(photoUrl))
            {
                photos.Remove(photoUrl);
                await _fileUploadService.DeleteFileAsync(photoUrl);
                branch.PhotosJson = JsonSerializer.Serialize(photos);
                await _context.SaveChangesAsync();
            }
        }

        return RedirectToAction(nameof(Edit), new { id = branchId });
    }
}
