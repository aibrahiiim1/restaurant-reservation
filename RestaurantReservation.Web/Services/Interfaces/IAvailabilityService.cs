using RestaurantReservation.Web.Models.Entities;

namespace RestaurantReservation.Web.Services.Interfaces;

public interface IAvailabilityService
{
    Task<List<TimeSlot>> GetAvailableTimeSlotsAsync(int branchId, DateTime date, int partySize);
    Task<List<Table>> GetAvailableTablesAsync(int branchId, DateTime date, TimeSpan time, int partySize, TableLocationType? locationType = null);
    Task<bool> IsTableAvailableAsync(int tableId, DateTime date, TimeSpan time, int durationMinutes, int? excludeBookingId = null);
    Task<int> GetBookingCountForSlotAsync(int branchId, DateTime date, TimeSpan time);
    Task<Dictionary<string, int>> GetAvailabilityCalendarAsync(int branchId, DateTime startDate, DateTime endDate, int partySize);
}
