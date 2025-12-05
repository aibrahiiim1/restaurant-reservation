using System.ComponentModel.DataAnnotations;

namespace RestaurantReservation.Web.Models.Entities;

public class Restaurant
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [MaxLength(500)]
    public string? LogoUrl { get; set; }
    
    [MaxLength(500)]
    public string? Website { get; set; }
    
    [MaxLength(100)]
    public string? Email { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<Branch> Branches { get; set; } = new List<Branch>();
    public virtual ICollection<Menu> Menus { get; set; } = new List<Menu>();
    public virtual ICollection<Offer> Offers { get; set; } = new List<Offer>();
}
