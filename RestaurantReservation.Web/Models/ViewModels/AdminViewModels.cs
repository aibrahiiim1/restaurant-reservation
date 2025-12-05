using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using RestaurantReservation.Web.Models.Entities;

namespace RestaurantReservation.Web.Models.ViewModels;

// Restaurant Admin ViewModels
public class RestaurantCreateViewModel
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    [Url]
    public string? Website { get; set; }

    [MaxLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    public IFormFile? Logo { get; set; }
}

public class RestaurantEditViewModel : RestaurantCreateViewModel
{
    public int Id { get; set; }
    public string? CurrentLogoUrl { get; set; }
    public bool IsActive { get; set; }
}

// Branch Admin ViewModels
public class BranchCreateViewModel
{
    public int RestaurantId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [MaxLength(20)]
    public string? ZipCode { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    [Phone]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [EmailAddress]
    [MaxLength(100)]
    public string? Email { get; set; }

    [Url]
    [MaxLength(500)]
    public string? Website { get; set; }

    [MaxLength(200)]
    public string? Cuisine { get; set; }

    [Range(1, 1000)]
    public int Capacity { get; set; } = 50;

    [MaxLength(100)]
    public string? Area { get; set; }

    public bool HasParking { get; set; }

    [MaxLength(200)]
    public string? PaymentOptions { get; set; }

    [MaxLength(100)]
    public string? DressCode { get; set; }

    public bool IsAccessible { get; set; }

    public bool IsChildFriendly { get; set; }

    [Range(15, 120)]
    public int BookingIntervalMinutes { get; set; } = 30;

    [Range(0, 168)]
    public int CancellationPolicyHours { get; set; } = 24;

    [Range(0, 10000)]
    public decimal? MinimumCharge { get; set; }

    public bool RequireDeposit { get; set; }

    [Range(0, 10000)]
    public decimal? DepositAmount { get; set; }

    public IFormFile? Logo { get; set; }

    public List<IFormFile>? Photos { get; set; }

    // Operating hours as JSON string
    public string? OperatingHoursJson { get; set; }

    // Closed days as comma-separated dates
    public string? ClosedDaysInput { get; set; }

    // For dropdown
    public SelectList? RestaurantList { get; set; }
}

public class BranchEditViewModel : BranchCreateViewModel
{
    public int Id { get; set; }
    public string? CurrentLogoUrl { get; set; }
    public List<string> CurrentPhotoUrls { get; set; } = new();
    public bool IsActive { get; set; }
}

// Table Admin ViewModels
public class TableCreateViewModel
{
    public int BranchId { get; set; }

    [Required]
    [MaxLength(50)]
    [Display(Name = "Table Number")]
    public string TableNumber { get; set; } = string.Empty;

    [Range(1, 50)]
    [Display(Name = "Minimum Capacity")]
    public int MinCapacity { get; set; } = 1;

    [Range(1, 50)]
    [Display(Name = "Maximum Capacity")]
    public int MaxCapacity { get; set; } = 4;

    [Display(Name = "Location Type")]
    public TableLocationType LocationType { get; set; } = TableLocationType.Standard;

    [MaxLength(500)]
    public string? Description { get; set; }

    public List<IFormFile>? Photos { get; set; }

    // Layout position
    public int? LayoutX { get; set; }
    public int? LayoutY { get; set; }
    public int? LayoutWidth { get; set; }
    public int? LayoutHeight { get; set; }

    public string BranchName { get; set; } = string.Empty;
}

public class TableEditViewModel : TableCreateViewModel
{
    public int Id { get; set; }
    public List<string> CurrentPhotoUrls { get; set; } = new();
    public bool IsActive { get; set; }
}

// Time Slot Admin ViewModels
public class TimeSlotCreateViewModel
{
    public int BranchId { get; set; }

    [Display(Name = "Meal Type")]
    public MealType MealType { get; set; } = MealType.AllDay;

    [Required]
    [Display(Name = "Start Time")]
    public TimeSpan StartTime { get; set; }

    [Required]
    [Display(Name = "End Time")]
    public TimeSpan EndTime { get; set; }

    [Display(Name = "Day of Week (leave empty for all days)")]
    public int? DayOfWeek { get; set; }

    [Range(1, 100)]
    [Display(Name = "Maximum Bookings")]
    public int MaxBookings { get; set; } = 10;

    public string BranchName { get; set; } = string.Empty;
}

public class TimeSlotEditViewModel : TimeSlotCreateViewModel
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
}

// Menu Admin ViewModels
public class MenuCreateViewModel
{
    public int? RestaurantId { get; set; }
    public int? BranchId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public SelectList? RestaurantList { get; set; }
    public SelectList? BranchList { get; set; }
}

public class MenuEditViewModel : MenuCreateViewModel
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
}

public class MenuCategoryCreateViewModel
{
    public int MenuId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public string MenuName { get; set; } = string.Empty;
}

public class MenuItemCreateViewModel
{
    public int CategoryId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [Range(0, 10000)]
    public decimal Price { get; set; }

    public IFormFile? Photo { get; set; }

    public bool IsVegetarian { get; set; }
    public bool IsVegan { get; set; }
    public bool IsGlutenFree { get; set; }
    public bool IsHalal { get; set; }
    public bool IsKosher { get; set; }
    public bool ContainsNuts { get; set; }
    public bool ContainsDairy { get; set; }
    public bool IsSpicy { get; set; }

    [MaxLength(500)]
    public string? Allergens { get; set; }

    [Range(0, 5000)]
    public int CalorieCount { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool IsPopular { get; set; }

    public string CategoryName { get; set; } = string.Empty;
}

public class MenuItemEditViewModel : MenuItemCreateViewModel
{
    public int Id { get; set; }
    public string? CurrentPhotoUrl { get; set; }
}

// Booking Admin ViewModels
public class BookingListViewModel
{
    public int Id { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string GuestEmail { get; set; } = string.Empty;
    public int PartySize { get; set; }
    public DateTime BookingDate { get; set; }
    public TimeSpan BookingTime { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BookingAdminEditViewModel
{
    public int Id { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public int BranchId { get; set; }

    [Required]
    public int TableId { get; set; }

    [Required]
    public string GuestName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string GuestEmail { get; set; } = string.Empty;

    [Phone]
    public string? GuestPhone { get; set; }

    [Required]
    [Range(1, 50)]
    public int PartySize { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime BookingDate { get; set; }

    [Required]
    public TimeSpan BookingTime { get; set; }

    [Range(30, 480)]
    public int DurationMinutes { get; set; }

    public BookingStatus Status { get; set; }

    public OccasionType Occasion { get; set; }

    [MaxLength(1000)]
    public string? SpecialRequests { get; set; }

    public SelectList? TableList { get; set; }
    public SelectList? StatusList { get; set; }
}

// Review Admin ViewModels
public class ReviewAdminViewModel
{
    public int Id { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsApproved { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ReviewModerationViewModel
{
    public int Id { get; set; }
    public bool IsApproved { get; set; }
    public bool IsVisible { get; set; }

    [MaxLength(500)]
    public string? AdminResponse { get; set; }
}

// Offer Admin ViewModels
public class OfferCreateViewModel
{
    public int? RestaurantId { get; set; }
    public int? BranchId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public OfferType Type { get; set; }

    [Range(0, 10000)]
    public decimal? DiscountValue { get; set; }

    [Range(0, 10000)]
    public decimal? MinimumOrderAmount { get; set; }

    public int? LoyaltyPointsRequired { get; set; }
    public int? LoyaltyPointsBonus { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(1);

    public TimeSpan? ValidFromTime { get; set; }
    public TimeSpan? ValidToTime { get; set; }

    public int? MaxUsages { get; set; }

    public IFormFile? Image { get; set; }

    public SelectList? RestaurantList { get; set; }
    public SelectList? BranchList { get; set; }
}

public class OfferEditViewModel : OfferCreateViewModel
{
    public int Id { get; set; }
    public string? CurrentImageUrl { get; set; }
    public int UsageCount { get; set; }
    public bool IsActive { get; set; }
}

// Coupon Admin ViewModels
public class CouponCreateViewModel
{
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    public int? RestaurantId { get; set; }
    public int? BranchId { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    public OfferType Type { get; set; }

    [Range(0, 10000)]
    public decimal? DiscountValue { get; set; }

    [Range(0, 10000)]
    public decimal? MinimumOrderAmount { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(1);

    public int? MaxUsages { get; set; }

    public SelectList? RestaurantList { get; set; }
    public SelectList? BranchList { get; set; }
}

public class CouponEditViewModel : CouponCreateViewModel
{
    public int Id { get; set; }
    public int UsageCount { get; set; }
    public bool IsActive { get; set; }
}

// Dashboard ViewModels
public class AdminDashboardViewModel
{
    public int TotalRestaurants { get; set; }
    public int TotalBranches { get; set; }
    public int TotalBookings { get; set; }
    public int TodayBookings { get; set; }
    public int PendingBookings { get; set; }
    public int PendingReviews { get; set; }
    public decimal TodayRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public List<BookingListViewModel> RecentBookings { get; set; } = new();
    public List<ReviewAdminViewModel> RecentReviews { get; set; } = new();
}

public class BranchDashboardViewModel
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int TodayBookings { get; set; }
    public int WeekBookings { get; set; }
    public int PendingBookings { get; set; }
    public int ConfirmedBookings { get; set; }
    public double OccupancyRate { get; set; }
    public double AverageRating { get; set; }
    public List<BookingListViewModel> TodayBookingsList { get; set; } = new();
    public Dictionary<string, int> BookingsByHour { get; set; } = new();
}
