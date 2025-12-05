using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace RestaurantReservation.Web.Models.Entities;

public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    public string? FirstName { get; set; }
    
    [MaxLength(100)]
    public string? LastName { get; set; }
    
    [MaxLength(500)]
    public string? ProfilePictureUrl { get; set; }
    
    // For restaurant/branch managers
    public int? RestaurantId { get; set; }
    
    public int? BranchId { get; set; }
    
    // Loyalty program
    public int LoyaltyPoints { get; set; }
    
    // OTP verification
    [MaxLength(10)]
    public string? OtpCode { get; set; }
    
    public DateTime? OtpExpiresAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation properties
    public virtual Restaurant? Restaurant { get; set; }
    public virtual Branch? Branch { get; set; }
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();
}
