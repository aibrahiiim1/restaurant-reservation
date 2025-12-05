using Microsoft.EntityFrameworkCore;
using RestaurantReservation.Web.Data;
using RestaurantReservation.Web.Models.Entities;
using RestaurantReservation.Web.Services.Interfaces;

namespace RestaurantReservation.Web.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AvailabilityService> _logger;

    public AvailabilityService(ApplicationDbContext context, ILogger<AvailabilityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<TimeSlot>> GetAvailableTimeSlotsAsync(int branchId, DateTime date, int partySize)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        
        // Get all active time slots for this branch and day
        var timeSlots = await _context.TimeSlots
            .Where(ts => ts.BranchId == branchId && ts.IsActive)
            .Where(ts => ts.DayOfWeek == null || ts.DayOfWeek == dayOfWeek)
            .OrderBy(ts => ts.StartTime)
            .ToListAsync();

        // Get tables that can accommodate the party size
        var suitableTables = await _context.Tables
            .Where(t => t.BranchId == branchId && t.IsActive)
            .Where(t => t.MinCapacity <= partySize && t.MaxCapacity >= partySize)
            .ToListAsync();

        if (!suitableTables.Any())
        {
            return new List<TimeSlot>();
        }

        var availableSlots = new List<TimeSlot>();

        foreach (var slot in timeSlots)
        {
            // Check if any table is available for this slot
            foreach (var table in suitableTables)
            {
                var isAvailable = await IsTableAvailableAsync(table.Id, date, slot.StartTime, 90);
                if (isAvailable)
                {
                    availableSlots.Add(slot);
                    break; // At least one table is available, so slot is available
                }
            }
        }

        return availableSlots;
    }

    public async Task<List<Table>> GetAvailableTablesAsync(int branchId, DateTime date, TimeSpan time, int partySize, TableLocationType? locationType = null)
    {
        var query = _context.Tables
            .Where(t => t.BranchId == branchId && t.IsActive)
            .Where(t => t.MinCapacity <= partySize && t.MaxCapacity >= partySize);

        if (locationType.HasValue)
        {
            query = query.Where(t => t.LocationType == locationType.Value);
        }

        var suitableTables = await query.ToListAsync();
        var availableTables = new List<Table>();

        foreach (var table in suitableTables)
        {
            var isAvailable = await IsTableAvailableAsync(table.Id, date, time, 90);
            if (isAvailable)
            {
                availableTables.Add(table);
            }
        }

        return availableTables;
    }

    public async Task<bool> IsTableAvailableAsync(int tableId, DateTime date, TimeSpan time, int durationMinutes, int? excludeBookingId = null)
    {
        var bookingStart = date.Date.Add(time);
        var bookingEnd = bookingStart.AddMinutes(durationMinutes);

        var query = _context.Bookings
            .Where(b => b.TableId == tableId)
            .Where(b => b.Status != BookingStatus.Cancelled)
            .Where(b => b.BookingDate.Date == date.Date);

        if (excludeBookingId.HasValue)
        {
            query = query.Where(b => b.Id != excludeBookingId.Value);
        }

        var existingBookings = await query.ToListAsync();

        foreach (var booking in existingBookings)
        {
            var existingStart = booking.BookingDate.Date.Add(booking.BookingTime);
            var existingEnd = existingStart.AddMinutes(booking.DurationMinutes);

            // Check for overlap
            if (bookingStart < existingEnd && bookingEnd > existingStart)
            {
                return false;
            }
        }

        return true;
    }

    public async Task<int> GetBookingCountForSlotAsync(int branchId, DateTime date, TimeSpan time)
    {
        return await _context.Bookings
            .Where(b => b.BranchId == branchId)
            .Where(b => b.BookingDate.Date == date.Date)
            .Where(b => b.BookingTime == time)
            .Where(b => b.Status != BookingStatus.Cancelled)
            .CountAsync();
    }

    public async Task<Dictionary<string, int>> GetAvailabilityCalendarAsync(int branchId, DateTime startDate, DateTime endDate, int partySize)
    {
        var result = new Dictionary<string, int>();
        var currentDate = startDate.Date;

        while (currentDate <= endDate.Date)
        {
            var availableSlots = await GetAvailableTimeSlotsAsync(branchId, currentDate, partySize);
            result[currentDate.ToString("yyyy-MM-dd")] = availableSlots.Count;
            currentDate = currentDate.AddDays(1);
        }

        return result;
    }
}
