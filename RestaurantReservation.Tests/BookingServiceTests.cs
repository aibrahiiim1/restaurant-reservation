using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using RestaurantReservation.Web.Data;
using RestaurantReservation.Web.Models.Entities;
using RestaurantReservation.Web.Services;
using RestaurantReservation.Web.Services.Interfaces;
using Xunit;

namespace RestaurantReservation.Tests;

public class BookingServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly BookingService _bookingService;
    private readonly Mock<IAvailabilityService> _availabilityServiceMock;
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<BookingService>> _loggerMock;

    public BookingServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _availabilityServiceMock = new Mock<IAvailabilityService>();
        _paymentServiceMock = new Mock<IPaymentService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<BookingService>>();

        _bookingService = new BookingService(
            _context,
            _availabilityServiceMock.Object,
            _paymentServiceMock.Object,
            _notificationServiceMock.Object,
            _loggerMock.Object);

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
            CancellationPolicyHours = 24,
            IsActive = true
        };
        _context.Branches.Add(branch);

        var table = new Table
        {
            Id = 1,
            BranchId = 1,
            TableNumber = "T01",
            MinCapacity = 2,
            MaxCapacity = 4,
            IsActive = true
        };
        _context.Tables.Add(table);

        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateBookingAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        _availabilityServiceMock
            .Setup(x => x.IsTableAvailableAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<int?>()))
            .ReturnsAsync(true);

        var request = new BookingRequest
        {
            BranchId = 1,
            TableId = 1,
            GuestName = "John Doe",
            GuestEmail = "john@example.com",
            PartySize = 2,
            BookingDate = DateTime.Today.AddDays(1),
            BookingTime = new TimeSpan(18, 0, 0),
            DurationMinutes = 90
        };

        // Act
        var result = await _bookingService.CreateBookingAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Booking);
        Assert.Equal("John Doe", result.Booking.GuestName);
        Assert.Equal(BookingStatus.Pending, result.Booking.Status);
        Assert.NotEmpty(result.Booking.BookingReference);
    }

    [Fact]
    public async Task CreateBookingAsync_WithInvalidTable_ReturnsFailure()
    {
        // Arrange
        var request = new BookingRequest
        {
            BranchId = 1,
            TableId = 999, // Non-existent table
            GuestName = "John Doe",
            GuestEmail = "john@example.com",
            PartySize = 2,
            BookingDate = DateTime.Today.AddDays(1),
            BookingTime = new TimeSpan(18, 0, 0)
        };

        // Act
        var result = await _bookingService.CreateBookingAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage?.ToLower());
    }

    [Fact]
    public async Task CreateBookingAsync_WithInvalidPartySize_ReturnsFailure()
    {
        // Arrange
        _availabilityServiceMock
            .Setup(x => x.IsTableAvailableAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<int?>()))
            .ReturnsAsync(true);

        var request = new BookingRequest
        {
            BranchId = 1,
            TableId = 1,
            GuestName = "John Doe",
            GuestEmail = "john@example.com",
            PartySize = 10, // Exceeds table capacity of 4
            BookingDate = DateTime.Today.AddDays(1),
            BookingTime = new TimeSpan(18, 0, 0)
        };

        // Act
        var result = await _bookingService.CreateBookingAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("party size", result.ErrorMessage?.ToLower());
    }

    [Fact]
    public async Task CreateBookingAsync_WhenTableNotAvailable_ReturnsFailure()
    {
        // Arrange
        _availabilityServiceMock
            .Setup(x => x.IsTableAvailableAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<int?>()))
            .ReturnsAsync(false);

        var request = new BookingRequest
        {
            BranchId = 1,
            TableId = 1,
            GuestName = "John Doe",
            GuestEmail = "john@example.com",
            PartySize = 2,
            BookingDate = DateTime.Today.AddDays(1),
            BookingTime = new TimeSpan(18, 0, 0)
        };

        // Act
        var result = await _bookingService.CreateBookingAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not available", result.ErrorMessage?.ToLower());
    }

    [Fact]
    public async Task CancelBookingAsync_WithValidBooking_ReturnsSuccess()
    {
        // Arrange
        _availabilityServiceMock
            .Setup(x => x.IsTableAvailableAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<int?>()))
            .ReturnsAsync(true);

        var createRequest = new BookingRequest
        {
            BranchId = 1,
            TableId = 1,
            GuestName = "John Doe",
            GuestEmail = "john@example.com",
            PartySize = 2,
            BookingDate = DateTime.Today.AddDays(2),
            BookingTime = new TimeSpan(18, 0, 0)
        };
        
        var createResult = await _bookingService.CreateBookingAsync(createRequest);
        Assert.True(createResult.Success);

        // Act
        var cancelResult = await _bookingService.CancelBookingAsync(createResult.Booking!.Id, "Test cancellation");

        // Assert
        Assert.True(cancelResult.Success);
        Assert.Equal(BookingStatus.Cancelled, cancelResult.Booking?.Status);
        Assert.NotNull(cancelResult.Booking?.CancelledAt);
    }

    [Fact]
    public async Task GetBookingByReferenceAsync_WithValidReference_ReturnsBooking()
    {
        // Arrange
        _availabilityServiceMock
            .Setup(x => x.IsTableAvailableAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<int?>()))
            .ReturnsAsync(true);

        var createRequest = new BookingRequest
        {
            BranchId = 1,
            TableId = 1,
            GuestName = "Jane Doe",
            GuestEmail = "jane@example.com",
            PartySize = 2,
            BookingDate = DateTime.Today.AddDays(3),
            BookingTime = new TimeSpan(19, 0, 0)
        };
        
        var createResult = await _bookingService.CreateBookingAsync(createRequest);
        Assert.True(createResult.Success);

        // Act
        var booking = await _bookingService.GetBookingByReferenceAsync(createResult.Booking!.BookingReference);

        // Assert
        Assert.NotNull(booking);
        Assert.Equal("Jane Doe", booking.GuestName);
    }

    [Fact]
    public async Task GetBookingByReferenceAsync_WithInvalidReference_ReturnsNull()
    {
        // Act
        var booking = await _bookingService.GetBookingByReferenceAsync("INVALID123");

        // Assert
        Assert.Null(booking);
    }

    [Fact]
    public async Task ConfirmBookingAsync_WithPendingBooking_ReturnsSuccess()
    {
        // Arrange
        _availabilityServiceMock
            .Setup(x => x.IsTableAvailableAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<int?>()))
            .ReturnsAsync(true);

        var createRequest = new BookingRequest
        {
            BranchId = 1,
            TableId = 1,
            GuestName = "John Doe",
            GuestEmail = "john@example.com",
            PartySize = 2,
            BookingDate = DateTime.Today.AddDays(4),
            BookingTime = new TimeSpan(20, 0, 0)
        };
        
        var createResult = await _bookingService.CreateBookingAsync(createRequest);
        Assert.True(createResult.Success);

        // Act
        var confirmResult = await _bookingService.ConfirmBookingAsync(createResult.Booking!.Id);

        // Assert
        Assert.True(confirmResult.Success);
        Assert.Equal(BookingStatus.Confirmed, confirmResult.Booking?.Status);
        Assert.True(confirmResult.Booking?.IsVerified);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
