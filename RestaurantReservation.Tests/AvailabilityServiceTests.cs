using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RestaurantReservation.Web.Data;
using RestaurantReservation.Web.Models.Entities;
using RestaurantReservation.Web.Services;
using Xunit;

namespace RestaurantReservation.Tests;

public class AvailabilityServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AvailabilityService _availabilityService;
    private readonly Mock<ILogger<AvailabilityService>> _loggerMock;

    public AvailabilityServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<AvailabilityService>>();

        _availabilityService = new AvailabilityService(_context, _loggerMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var restaurant = new Restaurant
        {
            Id = 1,
            Name = "Test Restaurant",
            IsActive = true
        };
        _context.Restaurants.Add(restaurant);

        var branch = new Branch
        {
            Id = 1,
            RestaurantId = 1,
            Name = "Test Branch",
            Address = "123 Test St",
            Capacity = 50,
            BookingIntervalMinutes = 30,
            IsActive = true
        };
        _context.Branches.Add(branch);

        // Add tables with different capacities
        var tables = new List<Table>
        {
            new Table { Id = 1, BranchId = 1, TableNumber = "T01", MinCapacity = 2, MaxCapacity = 2, LocationType = TableLocationType.Indoor, IsActive = true },
            new Table { Id = 2, BranchId = 1, TableNumber = "T02", MinCapacity = 2, MaxCapacity = 4, LocationType = TableLocationType.Indoor, IsActive = true },
            new Table { Id = 3, BranchId = 1, TableNumber = "T03", MinCapacity = 4, MaxCapacity = 6, LocationType = TableLocationType.Outdoor, IsActive = true },
            new Table { Id = 4, BranchId = 1, TableNumber = "T04", MinCapacity = 6, MaxCapacity = 10, LocationType = TableLocationType.PrivateRoom, IsActive = true },
            new Table { Id = 5, BranchId = 1, TableNumber = "T05", MinCapacity = 2, MaxCapacity = 4, LocationType = TableLocationType.Terrace, IsActive = false } // Inactive
        };
        _context.Tables.AddRange(tables);

        // Add time slots
        var timeSlots = new List<TimeSlot>
        {
            new TimeSlot { Id = 1, BranchId = 1, MealType = MealType.Lunch, StartTime = new TimeSpan(12, 0, 0), EndTime = new TimeSpan(12, 30, 0), MaxBookings = 5, IsActive = true },
            new TimeSlot { Id = 2, BranchId = 1, MealType = MealType.Lunch, StartTime = new TimeSpan(12, 30, 0), EndTime = new TimeSpan(13, 0, 0), MaxBookings = 5, IsActive = true },
            new TimeSlot { Id = 3, BranchId = 1, MealType = MealType.Dinner, StartTime = new TimeSpan(18, 0, 0), EndTime = new TimeSpan(18, 30, 0), MaxBookings = 8, IsActive = true },
            new TimeSlot { Id = 4, BranchId = 1, MealType = MealType.Dinner, StartTime = new TimeSpan(19, 0, 0), EndTime = new TimeSpan(19, 30, 0), MaxBookings = 8, IsActive = true }
        };
        _context.TimeSlots.AddRange(timeSlots);

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetAvailableTimeSlotsAsync_WithValidData_ReturnsSlots()
    {
        // Arrange
        var date = DateTime.Today.AddDays(1);
        var partySize = 2;

        // Act
        var slots = await _availabilityService.GetAvailableTimeSlotsAsync(1, date, partySize);

        // Assert
        Assert.NotEmpty(slots);
        Assert.True(slots.Count >= 2); // Should have at least lunch and dinner slots
    }

    [Fact]
    public async Task GetAvailableTimeSlotsAsync_WithLargePartySize_ReturnsLimitedSlots()
    {
        // Arrange
        var date = DateTime.Today.AddDays(1);
        var partySize = 8; // Only table T04 can accommodate this

        // Act
        var slots = await _availabilityService.GetAvailableTimeSlotsAsync(1, date, partySize);

        // Assert
        Assert.NotEmpty(slots);
    }

    [Fact]
    public async Task GetAvailableTimeSlotsAsync_WithExcessivePartySize_ReturnsNoSlots()
    {
        // Arrange
        var date = DateTime.Today.AddDays(1);
        var partySize = 15; // No table can accommodate this

        // Act
        var slots = await _availabilityService.GetAvailableTimeSlotsAsync(1, date, partySize);

        // Assert
        Assert.Empty(slots);
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WithValidData_ReturnsAvailableTables()
    {
        // Arrange
        var date = DateTime.Today.AddDays(1);
        var time = new TimeSpan(18, 0, 0);
        var partySize = 2;

        // Act
        var tables = await _availabilityService.GetAvailableTablesAsync(1, date, time, partySize);

        // Assert
        Assert.NotEmpty(tables);
        Assert.All(tables, t => Assert.True(t.MinCapacity <= partySize && t.MaxCapacity >= partySize));
    }

    [Fact]
    public async Task GetAvailableTablesAsync_FilterByLocationType_ReturnsFilteredTables()
    {
        // Arrange
        var date = DateTime.Today.AddDays(1);
        var time = new TimeSpan(18, 0, 0);
        var partySize = 4;

        // Act
        var indoorTables = await _availabilityService.GetAvailableTablesAsync(1, date, time, partySize, TableLocationType.Indoor);
        var outdoorTables = await _availabilityService.GetAvailableTablesAsync(1, date, time, partySize, TableLocationType.Outdoor);

        // Assert
        Assert.All(indoorTables, t => Assert.Equal(TableLocationType.Indoor, t.LocationType));
        Assert.All(outdoorTables, t => Assert.Equal(TableLocationType.Outdoor, t.LocationType));
    }

    [Fact]
    public async Task IsTableAvailableAsync_WithNoConflicts_ReturnsTrue()
    {
        // Arrange
        var tableId = 1;
        var date = DateTime.Today.AddDays(1);
        var time = new TimeSpan(18, 0, 0);
        var duration = 90;

        // Act
        var isAvailable = await _availabilityService.IsTableAvailableAsync(tableId, date, time, duration);

        // Assert
        Assert.True(isAvailable);
    }

    [Fact]
    public async Task IsTableAvailableAsync_WithExistingBooking_ReturnsFalse()
    {
        // Arrange
        var tableId = 1;
        var date = DateTime.Today.AddDays(1);
        var time = new TimeSpan(18, 0, 0);
        var duration = 90;

        // Create an existing booking
        var existingBooking = new Booking
        {
            BranchId = 1,
            TableId = tableId,
            BookingReference = "TEST001",
            GuestName = "Existing Guest",
            GuestEmail = "existing@test.com",
            PartySize = 2,
            BookingDate = date,
            BookingTime = time,
            DurationMinutes = duration,
            Status = BookingStatus.Confirmed
        };
        _context.Bookings.Add(existingBooking);
        await _context.SaveChangesAsync();

        // Act
        var isAvailable = await _availabilityService.IsTableAvailableAsync(tableId, date, time, duration);

        // Assert
        Assert.False(isAvailable);
    }

    [Fact]
    public async Task IsTableAvailableAsync_WithOverlappingBooking_ReturnsFalse()
    {
        // Arrange
        var tableId = 2;
        var date = DateTime.Today.AddDays(2);

        // Create a booking from 18:00 to 19:30
        var existingBooking = new Booking
        {
            BranchId = 1,
            TableId = tableId,
            BookingReference = "TEST002",
            GuestName = "Existing Guest",
            GuestEmail = "existing@test.com",
            PartySize = 2,
            BookingDate = date,
            BookingTime = new TimeSpan(18, 0, 0),
            DurationMinutes = 90,
            Status = BookingStatus.Confirmed
        };
        _context.Bookings.Add(existingBooking);
        await _context.SaveChangesAsync();

        // Try to book at 18:30 (overlaps with existing booking)
        var newTime = new TimeSpan(18, 30, 0);

        // Act
        var isAvailable = await _availabilityService.IsTableAvailableAsync(tableId, date, newTime, 90);

        // Assert
        Assert.False(isAvailable);
    }

    [Fact]
    public async Task IsTableAvailableAsync_WithCancelledBooking_ReturnsTrue()
    {
        // Arrange
        var tableId = 3;
        var date = DateTime.Today.AddDays(3);
        var time = new TimeSpan(19, 0, 0);

        // Create a cancelled booking
        var cancelledBooking = new Booking
        {
            BranchId = 1,
            TableId = tableId,
            BookingReference = "TEST003",
            GuestName = "Cancelled Guest",
            GuestEmail = "cancelled@test.com",
            PartySize = 4,
            BookingDate = date,
            BookingTime = time,
            DurationMinutes = 90,
            Status = BookingStatus.Cancelled
        };
        _context.Bookings.Add(cancelledBooking);
        await _context.SaveChangesAsync();

        // Act
        var isAvailable = await _availabilityService.IsTableAvailableAsync(tableId, date, time, 90);

        // Assert
        Assert.True(isAvailable); // Should be available because existing booking is cancelled
    }

    [Fact]
    public async Task IsTableAvailableAsync_ExcludingCurrentBooking_ReturnsTrue()
    {
        // Arrange
        var tableId = 1;
        var date = DateTime.Today.AddDays(4);
        var time = new TimeSpan(20, 0, 0);

        // Create an existing booking
        var existingBooking = new Booking
        {
            BranchId = 1,
            TableId = tableId,
            BookingReference = "TEST004",
            GuestName = "Current Guest",
            GuestEmail = "current@test.com",
            PartySize = 2,
            BookingDate = date,
            BookingTime = time,
            DurationMinutes = 90,
            Status = BookingStatus.Confirmed
        };
        _context.Bookings.Add(existingBooking);
        await _context.SaveChangesAsync();

        // Act - checking availability excluding this booking (for modification)
        var isAvailable = await _availabilityService.IsTableAvailableAsync(tableId, date, time, 90, existingBooking.Id);

        // Assert
        Assert.True(isAvailable);
    }

    [Fact]
    public async Task GetBookingCountForSlotAsync_ReturnsCorrectCount()
    {
        // Arrange
        var date = DateTime.Today.AddDays(5);
        var time = new TimeSpan(18, 0, 0);

        // Add some bookings
        for (int i = 0; i < 3; i++)
        {
            _context.Bookings.Add(new Booking
            {
                BranchId = 1,
                TableId = i + 1,
                BookingReference = $"COUNT{i}",
                GuestName = $"Guest {i}",
                GuestEmail = $"guest{i}@test.com",
                PartySize = 2,
                BookingDate = date,
                BookingTime = time,
                DurationMinutes = 90,
                Status = BookingStatus.Confirmed
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var count = await _availabilityService.GetBookingCountForSlotAsync(1, date, time);

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task GetAvailabilityCalendarAsync_ReturnsDataForDateRange()
    {
        // Arrange
        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddDays(7);
        var partySize = 2;

        // Act
        var calendar = await _availabilityService.GetAvailabilityCalendarAsync(1, startDate, endDate, partySize);

        // Assert
        Assert.Equal(8, calendar.Count); // 8 days including start and end
        Assert.All(calendar.Values, slots => Assert.True(slots >= 0));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
