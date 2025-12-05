namespace RestaurantReservation.Web.Services.Interfaces;

public class PaymentResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? ClientSecret { get; set; }
}

public interface IPaymentService
{
    Task<PaymentResult> CreatePaymentIntentAsync(decimal amount, string currency, string description, Dictionary<string, string>? metadata = null);
    Task<PaymentResult> ConfirmPaymentAsync(string paymentIntentId);
    Task<PaymentResult> CancelPaymentAsync(string paymentIntentId);
    Task<PaymentResult> RefundPaymentAsync(string paymentIntentId, decimal? amount = null);
    Task<string?> GetPaymentStatusAsync(string paymentIntentId);
}
