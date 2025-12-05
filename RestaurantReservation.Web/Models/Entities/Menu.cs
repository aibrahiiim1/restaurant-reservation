using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantReservation.Web.Models.Entities;

public class Menu
{
    public int Id { get; set; }
    
    // Menu can belong to restaurant (global) or specific branch
    public int? RestaurantId { get; set; }
    
    public int? BranchId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual Restaurant? Restaurant { get; set; }
    public virtual Branch? Branch { get; set; }
    public virtual ICollection<MenuCategory> Categories { get; set; } = new List<MenuCategory>();
}

public class MenuCategory
{
    public int Id { get; set; }
    
    public int MenuId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public int DisplayOrder { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual Menu Menu { get; set; } = null!;
    public virtual ICollection<MenuItem> Items { get; set; } = new List<MenuItem>();
}

public class MenuItem
{
    public int Id { get; set; }
    
    public int CategoryId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }
    
    [MaxLength(500)]
    public string? PhotoUrl { get; set; }
    
    // Dietary flags
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
    
    public int CalorieCount { get; set; }
    
    public int DisplayOrder { get; set; }
    
    public bool IsAvailable { get; set; } = true;
    
    public bool IsPopular { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual MenuCategory Category { get; set; } = null!;
}
