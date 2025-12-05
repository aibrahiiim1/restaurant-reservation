using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantReservation.Web.Models.Entities;

public enum OfferType
{
    Percentage,
    FixedAmount,
    FreeItem,
    LoyaltyBonus
}

public class Offer
{
    public int Id { get; set; }
    
    // Offer can be restaurant-wide or branch-specific
    public int? RestaurantId { get; set; }
    
    public int? BranchId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public OfferType Type { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal? DiscountValue { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal? MinimumOrderAmount { get; set; }
    
    public int? LoyaltyPointsRequired { get; set; }
    
    public int? LoyaltyPointsBonus { get; set; }
    
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }
    
    // Days of week when offer is valid (JSON array)
    [MaxLength(100)]
    public string? ValidDaysJson { get; set; }
    
    // Time restrictions
    public TimeSpan? ValidFromTime { get; set; }
    
    public TimeSpan? ValidToTime { get; set; }
    
    public int? MaxUsages { get; set; }
    
    public int UsageCount { get; set; }
    
    [MaxLength(500)]
    public string? ImageUrl { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Restaurant? Restaurant { get; set; }
    public virtual Branch? Branch { get; set; }
}

public class Coupon
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;
    
    public int? RestaurantId { get; set; }
    
    public int? BranchId { get; set; }
    
    [MaxLength(200)]
    public string? Description { get; set; }
    
    public OfferType Type { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal? DiscountValue { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal? MinimumOrderAmount { get; set; }
    
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }
    
    public int? MaxUsages { get; set; }
    
    public int UsageCount { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

public class LoyaltyTransaction
{
    public int Id { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    
    public int? BookingId { get; set; }
    
    public int Points { get; set; }
    
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;
}
