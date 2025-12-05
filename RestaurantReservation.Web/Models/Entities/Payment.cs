using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantReservation.Web.Models.Entities;

public enum PaymentStatus
{
    Pending,
    Authorized,
    Captured,
    Failed,
    Refunded,
    Cancelled
}

public class Payment
{
    public int Id { get; set; }
    
    public int BookingId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string StripePaymentIntentId { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? StripeCustomerId { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }
    
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";
    
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(1000)]
    public string? MetadataJson { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual Booking Booking { get; set; } = null!;
}
