using RestaurantReservation.Web.Models.Entities;

namespace RestaurantReservation.Web.Services.Interfaces;

public interface INotificationService
{
    Task SendBookingConfirmationAsync(Booking booking);
    Task SendBookingReminderAsync(Booking booking);
    Task SendBookingCancellationAsync(Booking booking);
    Task SendBookingModificationAsync(Booking booking);
    Task SendOtpAsync(string recipient, string otp, NotificationChannel channel);
    Task SendReviewRequestAsync(Booking booking);
    Task NotifyAdminAsync(int branchId, string title, string message);
    Task<List<Notification>> GetUnreadNotificationsAsync(string userId);
    Task MarkNotificationAsReadAsync(int notificationId);
}
