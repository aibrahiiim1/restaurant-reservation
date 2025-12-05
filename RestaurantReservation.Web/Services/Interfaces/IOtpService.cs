namespace RestaurantReservation.Web.Services.Interfaces;

public class OtpResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IOtpService
{
    Task<OtpResult> SendOtpAsync(string userId, string? email = null, string? phone = null);
    Task<OtpResult> VerifyOtpAsync(string userId, string otp);
    Task<string> GenerateOtpAsync();
}
