using System.ComponentModel.DataAnnotations;
using RestaurantReservation.Web.Models.Entities;

namespace RestaurantReservation.Web.Models.ViewModels;

public class BookingViewModel
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string RestaurantName { get; set; } = string.Empty;

    [Required]
    [Range(1, 20)]
    [Display(Name = "Party Size")]
    public int PartySize { get; set; } = 2;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateTime BookingDate { get; set; } = DateTime.Today.AddDays(1);

    public TimeSpan? SelectedTime { get; set; }

    public int? SelectedTableId { get; set; }

    public TableLocationType? PreferredLocation { get; set; }

    public List<TimeSlotViewModel> AvailableTimeSlots { get; set; } = new();
    public List<TableViewModel> AvailableTables { get; set; } = new();
}

public class BookingDetailsViewModel
{
    public int BookingId { get; set; }

    [Required]
    public int BranchId { get; set; }

    [Required]
    public int TableId { get; set; }

    [Required]
    [Display(Name = "Name")]
    public string GuestName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string GuestEmail { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Phone")]
    public string? GuestPhone { get; set; }

    [Required]
    [Range(1, 20)]
    [Display(Name = "Party Size")]
    public int PartySize { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime BookingDate { get; set; }

    [Required]
    public TimeSpan BookingTime { get; set; }

    [Display(Name = "Duration (minutes)")]
    public int DurationMinutes { get; set; } = 90;

    [Display(Name = "Occasion")]
    public OccasionType Occasion { get; set; } = OccasionType.None;

    [MaxLength(1000)]
    [Display(Name = "Special Requests")]
    public string? SpecialRequests { get; set; }

    [Display(Name = "Coupon Code")]
    public string? CouponCode { get; set; }

    // Branch info for display
    public string BranchName { get; set; } = string.Empty;
    public string RestaurantName { get; set; } = string.Empty;
    public string TableNumber { get; set; } = string.Empty;
    public TableLocationType TableLocation { get; set; }

    // Payment info
    public bool RequireDeposit { get; set; }
    public decimal? DepositAmount { get; set; }
    public string? PaymentIntentClientSecret { get; set; }
}

public class BookingConfirmationViewModel
{
    public string BookingReference { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string RestaurantName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public TimeSpan BookingTime { get; set; }
    public int PartySize { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public string? QrCodeUrl { get; set; }
    public BookingStatus Status { get; set; }
    public bool CanCancel { get; set; }
    public bool CanModify { get; set; }
}

public class TimeSlotViewModel
{
    public int Id { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public MealType MealType { get; set; }
    public bool IsAvailable { get; set; }
    public int AvailableTablesCount { get; set; }
}

public class TableViewModel
{
    public int Id { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public int MinCapacity { get; set; }
    public int MaxCapacity { get; set; }
    public TableLocationType LocationType { get; set; }
    public string? Description { get; set; }
    public List<string> PhotoUrls { get; set; } = new();
}

public class CalendarDayViewModel
{
    public DateTime Date { get; set; }
    public int AvailableSlots { get; set; }
    public bool IsClosed { get; set; }
}
