using Microsoft.AspNetCore.SignalR;
using RestaurantReservation.Web.Data;
using RestaurantReservation.Web.Hubs;
using RestaurantReservation.Web.Models.Entities;
using RestaurantReservation.Web.Services.Interfaces;

namespace RestaurantReservation.Web.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;
    private readonly IConfiguration _configuration;

    public NotificationService(
        ApplicationDbContext context,
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendBookingConfirmationAsync(Booking booking)
    {
        var notification = new Notification
        {
            UserId = booking.UserId,
            BranchId = booking.BranchId,
            BookingId = booking.Id,
            Type = NotificationType.BookingConfirmation,
            Channel = NotificationChannel.Email,
            Title = "Booking Confirmation",
            Message = $"Your booking at {booking.Branch?.Name ?? "Restaurant"} for {booking.BookingDate:MMMM dd, yyyy} at {booking.BookingTime:hh\\:mm tt} has been confirmed. Reference: {booking.BookingReference}",
            RecipientEmail = booking.GuestEmail,
            RecipientPhone = booking.GuestPhone,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send email via SendGrid (placeholder)
        await SendEmailAsync(booking.GuestEmail, notification.Title, notification.Message);

        // Notify admin via SignalR
        await NotifyAdminAsync(booking.BranchId, "New Booking", $"New booking received: {booking.BookingReference}");

        _logger.LogInformation("Booking confirmation sent for {BookingReference}", booking.BookingReference);
    }

    public async Task SendBookingReminderAsync(Booking booking)
    {
        var notification = new Notification
        {
            UserId = booking.UserId,
            BranchId = booking.BranchId,
            BookingId = booking.Id,
            Type = NotificationType.BookingReminder,
            Channel = NotificationChannel.Email,
            Title = "Booking Reminder",
            Message = $"Reminder: Your reservation at {booking.Branch?.Name ?? "Restaurant"} is tomorrow at {booking.BookingTime:hh\\:mm tt}. Reference: {booking.BookingReference}",
            RecipientEmail = booking.GuestEmail,
            RecipientPhone = booking.GuestPhone,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        await SendEmailAsync(booking.GuestEmail, notification.Title, notification.Message);

        _logger.LogInformation("Booking reminder sent for {BookingReference}", booking.BookingReference);
    }

    public async Task SendBookingCancellationAsync(Booking booking)
    {
        var notification = new Notification
        {
            UserId = booking.UserId,
            BranchId = booking.BranchId,
            BookingId = booking.Id,
            Type = NotificationType.BookingCancellation,
            Channel = NotificationChannel.Email,
            Title = "Booking Cancelled",
            Message = $"Your booking at {booking.Branch?.Name ?? "Restaurant"} for {booking.BookingDate:MMMM dd, yyyy} has been cancelled. Reference: {booking.BookingReference}",
            RecipientEmail = booking.GuestEmail,
            RecipientPhone = booking.GuestPhone,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        await SendEmailAsync(booking.GuestEmail, notification.Title, notification.Message);
        await NotifyAdminAsync(booking.BranchId, "Booking Cancelled", $"Booking cancelled: {booking.BookingReference}");

        _logger.LogInformation("Booking cancellation sent for {BookingReference}", booking.BookingReference);
    }

    public async Task SendBookingModificationAsync(Booking booking)
    {
        var notification = new Notification
        {
            UserId = booking.UserId,
            BranchId = booking.BranchId,
            BookingId = booking.Id,
            Type = NotificationType.BookingModification,
            Channel = NotificationChannel.Email,
            Title = "Booking Modified",
            Message = $"Your booking at {booking.Branch?.Name ?? "Restaurant"} has been modified. New date: {booking.BookingDate:MMMM dd, yyyy} at {booking.BookingTime:hh\\:mm tt}. Reference: {booking.BookingReference}",
            RecipientEmail = booking.GuestEmail,
            RecipientPhone = booking.GuestPhone,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        await SendEmailAsync(booking.GuestEmail, notification.Title, notification.Message);
        await NotifyAdminAsync(booking.BranchId, "Booking Modified", $"Booking modified: {booking.BookingReference}");

        _logger.LogInformation("Booking modification sent for {BookingReference}", booking.BookingReference);
    }

    public async Task SendOtpAsync(string recipient, string otp, NotificationChannel channel)
    {
        var message = $"Your verification code is: {otp}. This code expires in 10 minutes.";

        if (channel == NotificationChannel.Email)
        {
            await SendEmailAsync(recipient, "Verification Code", message);
        }
        else if (channel == NotificationChannel.SMS)
        {
            await SendSmsAsync(recipient, message);
        }

        _logger.LogInformation("OTP sent to {Recipient} via {Channel}", recipient, channel);
    }

    public async Task SendReviewRequestAsync(Booking booking)
    {
        var notification = new Notification
        {
            UserId = booking.UserId,
            BranchId = booking.BranchId,
            BookingId = booking.Id,
            Type = NotificationType.ReviewRequest,
            Channel = NotificationChannel.Email,
            Title = "How was your experience?",
            Message = $"We hope you enjoyed your visit to {booking.Branch?.Name ?? "Restaurant"}. Please take a moment to leave a review.",
            RecipientEmail = booking.GuestEmail,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        await SendEmailAsync(booking.GuestEmail, notification.Title, notification.Message);

        _logger.LogInformation("Review request sent for booking {BookingReference}", booking.BookingReference);
    }

    public async Task NotifyAdminAsync(int branchId, string title, string message)
    {
        await _hubContext.Clients.Group($"branch_{branchId}").SendAsync("ReceiveNotification", new
        {
            title,
            message,
            timestamp = DateTime.UtcNow
        });

        _logger.LogInformation("Admin notification sent to branch {BranchId}: {Title}", branchId, title);
    }

    public async Task<List<Notification>> GetUnreadNotificationsAsync(string userId)
    {
        return await Task.FromResult(_context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToList());
    }

    public async Task MarkNotificationAsReadAsync(int notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    private async Task SendEmailAsync(string to, string subject, string body)
    {
        // SendGrid integration placeholder
        var apiKey = _configuration["SendGrid:ApiKey"];
        
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("SendGrid API key not configured. Email not sent to {Email}", to);
            return;
        }

        // Implementation would use SendGrid SDK
        _logger.LogInformation("Email would be sent to {Email}: {Subject}", to, subject);
        await Task.CompletedTask;
    }

    private async Task SendSmsAsync(string to, string message)
    {
        // Twilio integration placeholder
        var accountSid = _configuration["Twilio:AccountSid"];
        var authToken = _configuration["Twilio:AuthToken"];
        
        if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken))
        {
            _logger.LogWarning("Twilio credentials not configured. SMS not sent to {Phone}", to);
            return;
        }

        // Implementation would use Twilio SDK
        _logger.LogInformation("SMS would be sent to {Phone}", to);
        await Task.CompletedTask;
    }
}
