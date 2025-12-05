using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantReservation.Web.Models.Entities;

public enum BookingStatus
{
    Pending,
    Confirmed,
    Cancelled,
    Completed,
    NoShow
}

public enum OccasionType
{
    None,
    Birthday,
    Anniversary,
    DateNight,
    BusinessMeeting,
    Celebration,
    Other
}

public class Booking
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string BookingReference { get; set; } = string.Empty;
    
    public int BranchId { get; set; }
    
    public int TableId { get; set; }
    
    public string? UserId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string GuestName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string GuestEmail { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? GuestPhone { get; set; }
    
    public int PartySize { get; set; }
    
    public DateTime BookingDate { get; set; }
    
    public TimeSpan BookingTime { get; set; }
    
    public int DurationMinutes { get; set; } = 90;
    
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    
    public OccasionType Occasion { get; set; } = OccasionType.None;
    
    [MaxLength(1000)]
    public string? SpecialRequests { get; set; }
    
    [MaxLength(500)]
    public string? QrCodeUrl { get; set; }
    
    // Payment info
    [Column(TypeName = "decimal(10,2)")]
    public decimal? DepositAmount { get; set; }
    
    public bool DepositPaid { get; set; }
    
    [MaxLength(100)]
    public string? StripePaymentIntentId { get; set; }
    
    // Applied offers/coupons
    public int? CouponId { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal? DiscountAmount { get; set; }
    
    // Loyalty points earned
    public int LoyaltyPointsEarned { get; set; }
    
    public bool IsVerified { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public DateTime? CancelledAt { get; set; }
    
    [MaxLength(500)]
    public string? CancellationReason { get; set; }
    
    // Concurrency token
    [Timestamp]
    public byte[]? RowVersion { get; set; }
    
    // Navigation properties
    public virtual Branch Branch { get; set; } = null!;
    public virtual Table Table { get; set; } = null!;
    public virtual ApplicationUser? User { get; set; }
    public virtual Coupon? Coupon { get; set; }
    public virtual Review? Review { get; set; }
}
