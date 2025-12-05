using System.ComponentModel.DataAnnotations;

namespace RestaurantReservation.Web.Models.Entities;

public enum TableLocationType
{
    Indoor,
    Outdoor,
    Terrace,
    Standard,
    PrivateRoom,
    Bar
}

public class Table
{
    public int Id { get; set; }
    
    public int BranchId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string TableNumber { get; set; } = string.Empty;
    
    public int MinCapacity { get; set; } = 1;
    
    public int MaxCapacity { get; set; } = 4;
    
    public TableLocationType LocationType { get; set; } = TableLocationType.Standard;
    
    // Visual layout position as JSON
    [MaxLength(1000)]
    public string? LayoutJson { get; set; }
    
    // Photos stored as JSON array of URLs
    [MaxLength(2000)]
    public string? PhotosJson { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Concurrency token
    [Timestamp]
    public byte[]? RowVersion { get; set; }
    
    // Navigation properties
    public virtual Branch Branch { get; set; } = null!;
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
