using System.ComponentModel.DataAnnotations;

namespace RestaurantReservation.Web.Models.Entities;

public enum NotificationType
{
    BookingConfirmation,
    BookingReminder,
    BookingCancellation,
    BookingModification,
    ReviewRequest,
    OfferAlert,
    SystemAlert
}

public enum NotificationChannel
{
    Email,
    SMS,
    Push,
    InApp
}

public class Notification
{
    public int Id { get; set; }
    
    public string? UserId { get; set; }
    
    public int? BranchId { get; set; }
    
    public int? BookingId { get; set; }
    
    public NotificationType Type { get; set; }
    
    public NotificationChannel Channel { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? RecipientEmail { get; set; }
    
    [MaxLength(20)]
    public string? RecipientPhone { get; set; }
    
    public bool IsSent { get; set; }
    
    public DateTime? SentAt { get; set; }
    
    public bool IsRead { get; set; }
    
    public DateTime? ReadAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ApplicationUser? User { get; set; }
    public virtual Branch? Branch { get; set; }
    public virtual Booking? Booking { get; set; }
}
