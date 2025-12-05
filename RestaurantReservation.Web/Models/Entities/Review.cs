using System.ComponentModel.DataAnnotations;

namespace RestaurantReservation.Web.Models.Entities;

public class Review
{
    public int Id { get; set; }
    
    public int BranchId { get; set; }
    
    public int? BookingId { get; set; }
    
    public string? UserId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string GuestName { get; set; } = string.Empty;
    
    [Range(1, 5)]
    public int Rating { get; set; }
    
    [MaxLength(2000)]
    public string? Comment { get; set; }
    
    // Sub-ratings
    [Range(1, 5)]
    public int? FoodRating { get; set; }
    
    [Range(1, 5)]
    public int? ServiceRating { get; set; }
    
    [Range(1, 5)]
    public int? AmbianceRating { get; set; }
    
    [Range(1, 5)]
    public int? ValueRating { get; set; }
    
    // Admin moderation
    public bool IsApproved { get; set; }
    
    public bool IsVisible { get; set; } = true;
    
    [MaxLength(500)]
    public string? AdminResponse { get; set; }
    
    public DateTime? AdminResponseAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual Branch Branch { get; set; } = null!;
    public virtual Booking? Booking { get; set; }
    public virtual ApplicationUser? User { get; set; }
}
