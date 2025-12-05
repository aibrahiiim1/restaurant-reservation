using System.ComponentModel.DataAnnotations;
using RestaurantReservation.Web.Models.Entities;

namespace RestaurantReservation.Web.Models.ViewModels;

public class RestaurantViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
    public bool IsActive { get; set; }
    public List<BranchSummaryViewModel> Branches { get; set; } = new();
}

public class RestaurantDetailViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
    public List<BranchViewModel> Branches { get; set; } = new();
    public List<Offer> ActiveOffers { get; set; } = new();
}

public class BranchSummaryViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Cuisine { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public string? PhotoUrl { get; set; }
}

public class BranchViewModel
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public List<string> PhotoUrls { get; set; } = new();
    public string? Cuisine { get; set; }
    public int Capacity { get; set; }
    public string? Area { get; set; }
    public bool HasParking { get; set; }
    public string? PaymentOptions { get; set; }
    public string? DressCode { get; set; }
    public bool IsAccessible { get; set; }
    public bool IsChildFriendly { get; set; }
    public Dictionary<string, OperatingHours>? OperatingHours { get; set; }
    public List<DateTime>? ClosedDays { get; set; }
    public int BookingIntervalMinutes { get; set; }
    public int CancellationPolicyHours { get; set; }
    public decimal? MinimumCharge { get; set; }
    public bool RequireDeposit { get; set; }
    public decimal? DepositAmount { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public List<ReviewViewModel> RecentReviews { get; set; } = new();
    public List<Offer> ActiveOffers { get; set; } = new();
    public List<MenuViewModel> Menus { get; set; } = new();
}

public class OperatingHours
{
    public string? Open { get; set; }
    public string? Close { get; set; }
    public bool Closed { get; set; }
}

public class ReviewViewModel
{
    public int Id { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public int? FoodRating { get; set; }
    public int? ServiceRating { get; set; }
    public int? AmbianceRating { get; set; }
    public int? ValueRating { get; set; }
    public string? AdminResponse { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MenuViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<MenuCategoryViewModel> Categories { get; set; } = new();
}

public class MenuCategoryViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<MenuItemViewModel> Items { get; set; } = new();
}

public class MenuItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsVegetarian { get; set; }
    public bool IsVegan { get; set; }
    public bool IsGlutenFree { get; set; }
    public bool IsSpicy { get; set; }
    public bool IsPopular { get; set; }
    public string? Allergens { get; set; }
    public int CalorieCount { get; set; }
}
