using RestaurantReservation.Web.Models.Entities;

namespace RestaurantReservation.Web.Services.Interfaces;

public class BookingResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Booking? Booking { get; set; }
    public string? PaymentIntentClientSecret { get; set; }
}

public class BookingRequest
{
    public int BranchId { get; set; }
    public int TableId { get; set; }
    public string? UserId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string GuestEmail { get; set; } = string.Empty;
    public string? GuestPhone { get; set; }
    public int PartySize { get; set; }
    public DateTime BookingDate { get; set; }
    public TimeSpan BookingTime { get; set; }
    public int DurationMinutes { get; set; } = 90;
    public OccasionType Occasion { get; set; } = OccasionType.None;
    public string? SpecialRequests { get; set; }
    public string? CouponCode { get; set; }
}

public interface IBookingService
{
    Task<BookingResult> CreateBookingAsync(BookingRequest request);
    Task<BookingResult> UpdateBookingAsync(int bookingId, BookingRequest request);
    Task<BookingResult> CancelBookingAsync(int bookingId, string? reason = null);
    Task<BookingResult> ConfirmBookingAsync(int bookingId);
    Task<Booking?> GetBookingByIdAsync(int bookingId);
    Task<Booking?> GetBookingByReferenceAsync(string reference);
    Task<List<Booking>> GetBookingsForBranchAsync(int branchId, DateTime? date = null, BookingStatus? status = null);
    Task<List<Booking>> GetBookingsForUserAsync(string userId);
    Task<bool> CanCancelBookingAsync(int bookingId);
    Task<bool> CanModifyBookingAsync(int bookingId);
    Task<string> GenerateQrCodeAsync(Booking booking);
}
