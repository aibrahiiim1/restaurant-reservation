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
[Authorize(Roles = "SuperAdmin,RestaurantManager")]
public class RestaurantsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<RestaurantsController> _logger;

    public RestaurantsController(
        ApplicationDbContext context,
        IFileUploadService fileUploadService,
        ILogger<RestaurantsController> logger)
    {
        _context = context;
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var restaurants = await _context.Restaurants
            .Include(r => r.Branches)
            .ToListAsync();

        return View(restaurants);
    }

    public async Task<IActionResult> Details(int id)
    {
        var restaurant = await _context.Restaurants
            .Include(r => r.Branches)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (restaurant == null)
        {
            return NotFound();
        }

        return View(restaurant);
    }

    [Authorize(Roles = "SuperAdmin")]
    public IActionResult Create()
    {
        return View(new RestaurantCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Create(RestaurantCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var restaurant = new Restaurant
        {
            Name = model.Name,
            Description = model.Description,
            Website = model.Website,
            Email = model.Email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        if (model.Logo != null)
        {
            restaurant.LogoUrl = await _fileUploadService.UploadFileAsync(model.Logo, "restaurants");
        }

        _context.Restaurants.Add(restaurant);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Restaurant created: {RestaurantName}", restaurant.Name);
        TempData["Success"] = "Restaurant created successfully";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var restaurant = await _context.Restaurants.FindAsync(id);
        if (restaurant == null)
        {
            return NotFound();
        }

        var viewModel = new RestaurantEditViewModel
        {
            Id = restaurant.Id,
            Name = restaurant.Name,
            Description = restaurant.Description,
            Website = restaurant.Website,
            Email = restaurant.Email,
            CurrentLogoUrl = restaurant.LogoUrl,
            IsActive = restaurant.IsActive
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RestaurantEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var restaurant = await _context.Restaurants.FindAsync(model.Id);
        if (restaurant == null)
        {
            return NotFound();
        }

        restaurant.Name = model.Name;
        restaurant.Description = model.Description;
        restaurant.Website = model.Website;
        restaurant.Email = model.Email;
        restaurant.IsActive = model.IsActive;
        restaurant.UpdatedAt = DateTime.UtcNow;

        if (model.Logo != null)
        {
            if (!string.IsNullOrEmpty(restaurant.LogoUrl))
            {
                await _fileUploadService.DeleteFileAsync(restaurant.LogoUrl);
            }
            restaurant.LogoUrl = await _fileUploadService.UploadFileAsync(model.Logo, "restaurants");
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Restaurant updated: {RestaurantName}", restaurant.Name);
        TempData["Success"] = "Restaurant updated successfully";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var restaurant = await _context.Restaurants
            .Include(r => r.Branches)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (restaurant == null)
        {
            return NotFound();
        }

        if (restaurant.Branches.Any())
        {
            TempData["Error"] = "Cannot delete restaurant with branches. Please delete branches first.";
            return RedirectToAction(nameof(Index));
        }

        if (!string.IsNullOrEmpty(restaurant.LogoUrl))
        {
            await _fileUploadService.DeleteFileAsync(restaurant.LogoUrl);
        }

        _context.Restaurants.Remove(restaurant);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Restaurant deleted: {RestaurantName}", restaurant.Name);
        TempData["Success"] = "Restaurant deleted successfully";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var restaurant = await _context.Restaurants.FindAsync(id);
        if (restaurant == null)
        {
            return NotFound();
        }

        restaurant.IsActive = !restaurant.IsActive;
        restaurant.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Restaurant {(restaurant.IsActive ? "activated" : "deactivated")} successfully";

        return RedirectToAction(nameof(Index));
    }
}
