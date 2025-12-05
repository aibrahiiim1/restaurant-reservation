using Microsoft.EntityFrameworkCore;
using RestaurantReservation.Web.Data;
using RestaurantReservation.Web.Models.Entities;
using RestaurantReservation.Web.Services.Interfaces;

namespace RestaurantReservation.Web.Services;

public class OtpService : IOtpService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<OtpService> _logger;
    private const int OtpLength = 6;
    private const int OtpExpirationMinutes = 10;

    public OtpService(
        ApplicationDbContext context,
        INotificationService notificationService,
        ILogger<OtpService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<OtpResult> SendOtpAsync(string userId, string? email = null, string? phone = null)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new OtpResult { Success = false, ErrorMessage = "User not found" };
            }

            var otp = await GenerateOtpAsync();
            user.OtpCode = otp;
            user.OtpExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpirationMinutes);

            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(email))
            {
                await _notificationService.SendOtpAsync(email, otp, NotificationChannel.Email);
            }
            else if (!string.IsNullOrEmpty(phone))
            {
                await _notificationService.SendOtpAsync(phone, otp, NotificationChannel.SMS);
            }
            else if (!string.IsNullOrEmpty(user.Email))
            {
                await _notificationService.SendOtpAsync(user.Email, otp, NotificationChannel.Email);
            }
            else if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                await _notificationService.SendOtpAsync(user.PhoneNumber, otp, NotificationChannel.SMS);
            }

            _logger.LogInformation("OTP sent for user {UserId}", userId);
            return new OtpResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending OTP for user {UserId}", userId);
            return new OtpResult { Success = false, ErrorMessage = "Failed to send OTP" };
        }
    }

    public async Task<OtpResult> VerifyOtpAsync(string userId, string otp)
    {
        try
        {
            var user = await _context.Users
                .OfType<ApplicationUser>()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return new OtpResult { Success = false, ErrorMessage = "User not found" };
            }

            if (string.IsNullOrEmpty(user.OtpCode) || user.OtpExpiresAt == null)
            {
                return new OtpResult { Success = false, ErrorMessage = "No OTP found. Please request a new one." };
            }

            if (DateTime.UtcNow > user.OtpExpiresAt)
            {
                return new OtpResult { Success = false, ErrorMessage = "OTP has expired. Please request a new one." };
            }

            if (user.OtpCode != otp)
            {
                return new OtpResult { Success = false, ErrorMessage = "Invalid OTP" };
            }

            // Clear OTP after successful verification
            user.OtpCode = null;
            user.OtpExpiresAt = null;
            user.PhoneNumberConfirmed = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("OTP verified for user {UserId}", userId);
            return new OtpResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying OTP for user {UserId}", userId);
            return new OtpResult { Success = false, ErrorMessage = "Failed to verify OTP" };
        }
    }

    public Task<string> GenerateOtpAsync()
    {
        var random = new Random();
        var otp = random.Next(0, (int)Math.Pow(10, OtpLength)).ToString().PadLeft(OtpLength, '0');
        return Task.FromResult(otp);
    }
}
