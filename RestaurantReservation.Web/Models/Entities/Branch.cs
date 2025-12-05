using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantReservation.Web.Models.Entities;

public class Branch
{
    public int Id { get; set; }
    
    public int RestaurantId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? City { get; set; }
    
    [MaxLength(100)]
    public string? State { get; set; }
    
    [MaxLength(20)]
    public string? ZipCode { get; set; }
    
    [MaxLength(100)]
    public string? Country { get; set; }
    
    public double? Latitude { get; set; }
    
    public double? Longitude { get; set; }
    
    [MaxLength(20)]
    public string? Phone { get; set; }
    
    [MaxLength(100)]
    public string? Email { get; set; }
    
    [MaxLength(500)]
    public string? Website { get; set; }
    
    [MaxLength(500)]
    public string? LogoUrl { get; set; }
    
    // Photos stored as JSON array of URLs
    [MaxLength(4000)]
    public string? PhotosJson { get; set; }
    
    [MaxLength(200)]
    public string? Cuisine { get; set; }
    
    public int Capacity { get; set; }
    
    [MaxLength(100)]
    public string? Area { get; set; }
    
    public bool HasParking { get; set; }
    
    [MaxLength(200)]
    public string? PaymentOptions { get; set; }
    
    [MaxLength(100)]
    public string? DressCode { get; set; }
    
    public bool IsAccessible { get; set; }
    
    public bool IsChildFriendly { get; set; }
    
    // Operating hours stored as JSON
    [MaxLength(2000)]
    public string? OperatingHoursJson { get; set; }
    
    // Closed days stored as JSON array
    [MaxLength(500)]
    public string? ClosedDaysJson { get; set; }
    
    // Booking settings
    public int BookingIntervalMinutes { get; set; } = 30;
    
    public int CancellationPolicyHours { get; set; } = 24;
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal? MinimumCharge { get; set; }
    
    public bool RequireDeposit { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal? DepositAmount { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Concurrency token
    [Timestamp]
    public byte[]? RowVersion { get; set; }
    
    // Navigation properties
    public virtual Restaurant Restaurant { get; set; } = null!;
    public virtual ICollection<Table> Tables { get; set; } = new List<Table>();
    public virtual ICollection<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public virtual ICollection<Menu> Menus { get; set; } = new List<Menu>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<Offer> Offers { get; set; } = new List<Offer>();
}
