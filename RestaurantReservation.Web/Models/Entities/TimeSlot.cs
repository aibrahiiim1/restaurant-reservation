using System.ComponentModel.DataAnnotations;

namespace RestaurantReservation.Web.Models.Entities;

public enum MealType
{
    Breakfast,
    Lunch,
    Dinner,
    Brunch,
    AllDay
}

public class TimeSlot
{
    public int Id { get; set; }
    
    public int BranchId { get; set; }
    
    public MealType MealType { get; set; } = MealType.AllDay;
    
    public TimeSpan StartTime { get; set; }
    
    public TimeSpan EndTime { get; set; }
    
    // Day of week (0 = Sunday, 6 = Saturday), null means all days
    public int? DayOfWeek { get; set; }
    
    public int MaxBookings { get; set; } = 10;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Branch Branch { get; set; } = null!;
}
