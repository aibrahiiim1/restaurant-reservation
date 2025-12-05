using RestaurantReservation.Web.Services.Interfaces;
using Stripe;

namespace RestaurantReservation.Web.Services;

public class PaymentService : IPaymentService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IConfiguration configuration, ILogger<PaymentService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        var stripeSecretKey = _configuration["Stripe:SecretKey"];
        if (!string.IsNullOrEmpty(stripeSecretKey))
        {
            StripeConfiguration.ApiKey = stripeSecretKey;
        }
    }

    public async Task<PaymentResult> CreatePaymentIntentAsync(decimal amount, string currency, string description, Dictionary<string, string>? metadata = null)
    {
        try
        {
            var stripeSecretKey = _configuration["Stripe:SecretKey"];
            if (string.IsNullOrEmpty(stripeSecretKey))
            {
                _logger.LogWarning("Stripe secret key not configured");
                return new PaymentResult
                {
                    Success = true,
                    PaymentIntentId = $"pi_mock_{Guid.NewGuid():N}",
                    ClientSecret = $"pi_mock_{Guid.NewGuid():N}_secret_{Guid.NewGuid():N}"
                };
            }

            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100), // Convert to cents
                Currency = currency.ToLowerInvariant(),
                Description = description,
                Metadata = metadata,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                }
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            _logger.LogInformation("PaymentIntent created: {PaymentIntentId}", paymentIntent.Id);

            return new PaymentResult
            {
                Success = true,
                PaymentIntentId = paymentIntent.Id,
                ClientSecret = paymentIntent.ClientSecret
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating PaymentIntent");
            return new PaymentResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PaymentIntent");
            return new PaymentResult
            {
                Success = false,
                ErrorMessage = "An error occurred while processing payment"
            };
        }
    }

    public async Task<PaymentResult> ConfirmPaymentAsync(string paymentIntentId)
    {
        try
        {
            if (paymentIntentId.StartsWith("pi_mock_"))
            {
                return new PaymentResult { Success = true, PaymentIntentId = paymentIntentId };
            }

            var service = new PaymentIntentService();
            var paymentIntent = await service.ConfirmAsync(paymentIntentId);

            return new PaymentResult
            {
                Success = paymentIntent.Status == "succeeded",
                PaymentIntentId = paymentIntent.Id
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error confirming PaymentIntent");
            return new PaymentResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<PaymentResult> CancelPaymentAsync(string paymentIntentId)
    {
        try
        {
            if (paymentIntentId.StartsWith("pi_mock_"))
            {
                return new PaymentResult { Success = true, PaymentIntentId = paymentIntentId };
            }

            var service = new PaymentIntentService();
            var paymentIntent = await service.CancelAsync(paymentIntentId);

            return new PaymentResult
            {
                Success = true,
                PaymentIntentId = paymentIntent.Id
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error cancelling PaymentIntent");
            return new PaymentResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<PaymentResult> RefundPaymentAsync(string paymentIntentId, decimal? amount = null)
    {
        try
        {
            if (paymentIntentId.StartsWith("pi_mock_"))
            {
                return new PaymentResult { Success = true, PaymentIntentId = paymentIntentId };
            }

            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId
            };

            if (amount.HasValue)
            {
                options.Amount = (long)(amount.Value * 100);
            }

            var service = new RefundService();
            var refund = await service.CreateAsync(options);

            return new PaymentResult
            {
                Success = refund.Status == "succeeded",
                PaymentIntentId = paymentIntentId
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating refund");
            return new PaymentResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<string?> GetPaymentStatusAsync(string paymentIntentId)
    {
        try
        {
            if (paymentIntentId.StartsWith("pi_mock_"))
            {
                return "succeeded";
            }

            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentIntentId);
            return paymentIntent.Status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status");
            return null;
        }
    }
}
