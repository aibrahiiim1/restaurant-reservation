using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantReservation.Web.Data;
using RestaurantReservation.Web.Models;
using RestaurantReservation.Web.Models.ViewModels;

namespace RestaurantReservation.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var restaurants = await _context.Restaurants
            .Include(r => r.Branches)
            .ThenInclude(b => b.Reviews)
            .Where(r => r.IsActive)
            .ToListAsync();

        var viewModels = restaurants.Select(r => new RestaurantViewModel
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            LogoUrl = r.LogoUrl,
            Website = r.Website,
            IsActive = r.IsActive,
            Branches = r.Branches.Where(b => b.IsActive).Select(b => new BranchSummaryViewModel
            {
                Id = b.Id,
                Name = b.Name,
                City = b.City,
                Cuisine = b.Cuisine,
                AverageRating = b.Reviews.Any() ? b.Reviews.Average(rev => rev.Rating) : 0,
                ReviewCount = b.Reviews.Count(rev => rev.IsVisible && rev.IsApproved),
                PhotoUrl = !string.IsNullOrEmpty(b.PhotosJson) 
                    ? JsonSerializer.Deserialize<List<string>>(b.PhotosJson)?.FirstOrDefault() 
                    : b.LogoUrl
            }).ToList()
        }).ToList();

        return View(viewModels);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
