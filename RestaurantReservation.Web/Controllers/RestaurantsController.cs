using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantReservation.Web.Data;
using RestaurantReservation.Web.Models.Entities;
using RestaurantReservation.Web.Models.ViewModels;

namespace RestaurantReservation.Web.Controllers;

public class RestaurantsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RestaurantsController> _logger;

    public RestaurantsController(ApplicationDbContext context, ILogger<RestaurantsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? cuisine, string? city, string? search)
    {
        var query = _context.Branches
            .Include(b => b.Restaurant)
            .Include(b => b.Reviews)
            .Where(b => b.IsActive && b.Restaurant.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(b => 
                b.Name.Contains(search) || 
                b.Restaurant.Name.Contains(search) ||
                (b.Cuisine != null && b.Cuisine.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(cuisine))
        {
            query = query.Where(b => b.Cuisine != null && b.Cuisine.Contains(cuisine));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(b => b.City == city);
        }

        var branches = await query.ToListAsync();

        var viewModels = branches.Select(b => new BranchSummaryViewModel
        {
            Id = b.Id,
            Name = $"{b.Restaurant.Name} - {b.Name}",
            City = b.City,
            Cuisine = b.Cuisine,
            AverageRating = b.Reviews.Any() ? b.Reviews.Average(r => r.Rating) : 0,
            ReviewCount = b.Reviews.Count(r => r.IsVisible && r.IsApproved),
            PhotoUrl = !string.IsNullOrEmpty(b.PhotosJson) 
                ? JsonSerializer.Deserialize<List<string>>(b.PhotosJson)?.FirstOrDefault() 
                : b.LogoUrl
        }).ToList();

        ViewBag.Cuisines = await _context.Branches
            .Where(b => b.IsActive && !string.IsNullOrEmpty(b.Cuisine))
            .Select(b => b.Cuisine)
            .Distinct()
            .ToListAsync();

        ViewBag.Cities = await _context.Branches
            .Where(b => b.IsActive && !string.IsNullOrEmpty(b.City))
            .Select(b => b.City)
            .Distinct()
            .ToListAsync();

        ViewBag.CurrentCuisine = cuisine;
        ViewBag.CurrentCity = city;
        ViewBag.SearchTerm = search;

        return View(viewModels);
    }

    public async Task<IActionResult> Details(int id)
    {
        var restaurant = await _context.Restaurants
            .Include(r => r.Branches.Where(b => b.IsActive))
            .ThenInclude(b => b.Reviews.Where(rev => rev.IsVisible && rev.IsApproved))
            .Include(r => r.Menus.Where(m => m.IsActive))
            .ThenInclude(m => m.Categories.Where(c => c.IsActive))
            .ThenInclude(c => c.Items.Where(i => i.IsAvailable))
            .Include(r => r.Offers.Where(o => o.IsActive && o.EndDate >= DateTime.UtcNow))
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

        if (restaurant == null)
        {
            return NotFound();
        }

        var viewModel = new RestaurantDetailViewModel
        {
            Id = restaurant.Id,
            Name = restaurant.Name,
            Description = restaurant.Description,
            LogoUrl = restaurant.LogoUrl,
            Website = restaurant.Website,
            Branches = restaurant.Branches.Select(b => MapToBranchViewModel(b)).ToList(),
            ActiveOffers = restaurant.Offers.ToList()
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Branch(int id)
    {
        var branch = await _context.Branches
            .Include(b => b.Restaurant)
            .Include(b => b.Reviews.Where(r => r.IsVisible && r.IsApproved))
            .Include(b => b.Menus.Where(m => m.IsActive))
            .ThenInclude(m => m.Categories.Where(c => c.IsActive))
            .ThenInclude(c => c.Items.Where(i => i.IsAvailable))
            .Include(b => b.Offers.Where(o => o.IsActive && o.EndDate >= DateTime.UtcNow))
            .FirstOrDefaultAsync(b => b.Id == id && b.IsActive);

        if (branch == null)
        {
            return NotFound();
        }

        // Also include restaurant-level menus
        var restaurantMenus = await _context.Menus
            .Include(m => m.Categories.Where(c => c.IsActive))
            .ThenInclude(c => c.Items.Where(i => i.IsAvailable))
            .Where(m => m.RestaurantId == branch.RestaurantId && m.BranchId == null && m.IsActive)
            .ToListAsync();

        var viewModel = MapToBranchViewModel(branch);
        
        // Add restaurant-level menus
        foreach (var menu in restaurantMenus)
        {
            viewModel.Menus.Add(MapToMenuViewModel(menu));
        }

        // Add restaurant-level offers
        var restaurantOffers = await _context.Offers
            .Where(o => o.RestaurantId == branch.RestaurantId && o.BranchId == null && o.IsActive && o.EndDate >= DateTime.UtcNow)
            .ToListAsync();
        viewModel.ActiveOffers.AddRange(restaurantOffers);

        return View(viewModel);
    }

    public async Task<IActionResult> Menu(int branchId)
    {
        var branch = await _context.Branches
            .Include(b => b.Restaurant)
            .FirstOrDefaultAsync(b => b.Id == branchId && b.IsActive);

        if (branch == null)
        {
            return NotFound();
        }

        var menus = await _context.Menus
            .Include(m => m.Categories.Where(c => c.IsActive))
            .ThenInclude(c => c.Items.Where(i => i.IsAvailable).OrderBy(i => i.DisplayOrder))
            .Where(m => (m.BranchId == branchId || (m.RestaurantId == branch.RestaurantId && m.BranchId == null)) && m.IsActive)
            .ToListAsync();

        var viewModels = menus.Select(m => MapToMenuViewModel(m)).ToList();

        ViewBag.BranchName = $"{branch.Restaurant.Name} - {branch.Name}";
        ViewBag.BranchId = branchId;

        return View(viewModels);
    }

    public async Task<IActionResult> Reviews(int branchId, int page = 1)
    {
        const int pageSize = 10;

        var branch = await _context.Branches
            .Include(b => b.Restaurant)
            .FirstOrDefaultAsync(b => b.Id == branchId && b.IsActive);

        if (branch == null)
        {
            return NotFound();
        }

        var totalReviews = await _context.Reviews
            .Where(r => r.BranchId == branchId && r.IsVisible && r.IsApproved)
            .CountAsync();

        var reviews = await _context.Reviews
            .Where(r => r.BranchId == branchId && r.IsVisible && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var viewModels = reviews.Select(r => new ReviewViewModel
        {
            Id = r.Id,
            GuestName = r.GuestName,
            Rating = r.Rating,
            Comment = r.Comment,
            FoodRating = r.FoodRating,
            ServiceRating = r.ServiceRating,
            AmbianceRating = r.AmbianceRating,
            ValueRating = r.ValueRating,
            AdminResponse = r.AdminResponse,
            CreatedAt = r.CreatedAt
        }).ToList();

        ViewBag.BranchName = $"{branch.Restaurant.Name} - {branch.Name}";
        ViewBag.BranchId = branchId;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalReviews / (double)pageSize);
        ViewBag.AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
        ViewBag.TotalReviews = totalReviews;

        return View(viewModels);
    }

    private BranchViewModel MapToBranchViewModel(Branch branch)
    {
        var photoUrls = new List<string>();
        if (!string.IsNullOrEmpty(branch.PhotosJson))
        {
            try
            {
                photoUrls = JsonSerializer.Deserialize<List<string>>(branch.PhotosJson) ?? new List<string>();
            }
            catch { }
        }

        Dictionary<string, OperatingHours>? operatingHours = null;
        if (!string.IsNullOrEmpty(branch.OperatingHoursJson))
        {
            try
            {
                operatingHours = JsonSerializer.Deserialize<Dictionary<string, OperatingHours>>(branch.OperatingHoursJson);
            }
            catch { }
        }

        List<DateTime>? closedDays = null;
        if (!string.IsNullOrEmpty(branch.ClosedDaysJson))
        {
            try
            {
                var dates = JsonSerializer.Deserialize<List<string>>(branch.ClosedDaysJson);
                closedDays = dates?.Select(d => DateTime.Parse(d)).ToList();
            }
            catch { }
        }

        return new BranchViewModel
        {
            Id = branch.Id,
            RestaurantId = branch.RestaurantId,
            RestaurantName = branch.Restaurant.Name,
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
            LogoUrl = branch.LogoUrl,
            PhotoUrls = photoUrls,
            Cuisine = branch.Cuisine,
            Capacity = branch.Capacity,
            Area = branch.Area,
            HasParking = branch.HasParking,
            PaymentOptions = branch.PaymentOptions,
            DressCode = branch.DressCode,
            IsAccessible = branch.IsAccessible,
            IsChildFriendly = branch.IsChildFriendly,
            OperatingHours = operatingHours,
            ClosedDays = closedDays,
            BookingIntervalMinutes = branch.BookingIntervalMinutes,
            CancellationPolicyHours = branch.CancellationPolicyHours,
            MinimumCharge = branch.MinimumCharge,
            RequireDeposit = branch.RequireDeposit,
            DepositAmount = branch.DepositAmount,
            AverageRating = branch.Reviews.Any() ? branch.Reviews.Average(r => r.Rating) : 0,
            ReviewCount = branch.Reviews.Count(r => r.IsVisible && r.IsApproved),
            RecentReviews = branch.Reviews
                .Where(r => r.IsVisible && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .Select(r => new ReviewViewModel
                {
                    Id = r.Id,
                    GuestName = r.GuestName,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    FoodRating = r.FoodRating,
                    ServiceRating = r.ServiceRating,
                    AmbianceRating = r.AmbianceRating,
                    ValueRating = r.ValueRating,
                    AdminResponse = r.AdminResponse,
                    CreatedAt = r.CreatedAt
                }).ToList(),
            ActiveOffers = branch.Offers.ToList(),
            Menus = branch.Menus.Select(m => MapToMenuViewModel(m)).ToList()
        };
    }

    private MenuViewModel MapToMenuViewModel(Menu menu)
    {
        return new MenuViewModel
        {
            Id = menu.Id,
            Name = menu.Name,
            Description = menu.Description,
            Categories = menu.Categories.OrderBy(c => c.DisplayOrder).Select(c => new MenuCategoryViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Items = c.Items.OrderBy(i => i.DisplayOrder).Select(i => new MenuItemViewModel
                {
                    Id = i.Id,
                    Name = i.Name,
                    Description = i.Description,
                    Price = i.Price,
                    PhotoUrl = i.PhotoUrl,
                    IsVegetarian = i.IsVegetarian,
                    IsVegan = i.IsVegan,
                    IsGlutenFree = i.IsGlutenFree,
                    IsSpicy = i.IsSpicy,
                    IsPopular = i.IsPopular,
                    Allergens = i.Allergens,
                    CalorieCount = i.CalorieCount
                }).ToList()
            }).ToList()
        };
    }
}
