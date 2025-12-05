using Microsoft.EntityFrameworkCore;
using QRCoder;
using RestaurantReservation.Web.Data;
using RestaurantReservation.Web.Models.Entities;
using RestaurantReservation.Web.Services.Interfaces;

namespace RestaurantReservation.Web.Services;

public class BookingService : IBookingService
{
    private readonly ApplicationDbContext _context;
    private readonly IAvailabilityService _availabilityService;
    private readonly IPaymentService _paymentService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        ApplicationDbContext context,
        IAvailabilityService availabilityService,
        IPaymentService paymentService,
        INotificationService notificationService,
        ILogger<BookingService> logger)
    {
        _context = context;
        _availabilityService = availabilityService;
        _paymentService = paymentService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<BookingResult> CreateBookingAsync(BookingRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Validate table exists and is active
            var table = await _context.Tables
                .Include(t => t.Branch)
                .FirstOrDefaultAsync(t => t.Id == request.TableId && t.IsActive);

            if (table == null)
            {
                return new BookingResult { Success = false, ErrorMessage = "Table not found or inactive." };
            }

            // Validate party size
            if (request.PartySize < table.MinCapacity || request.PartySize > table.MaxCapacity)
            {
                return new BookingResult 
                { 
                    Success = false, 
                    ErrorMessage = $"Party size must be between {table.MinCapacity} and {table.MaxCapacity} for this table." 
                };
            }

            // Check availability with optimistic concurrency
            var isAvailable = await _availabilityService.IsTableAvailableAsync(
                request.TableId, 
                request.BookingDate, 
                request.BookingTime, 
                request.DurationMinutes);

            if (!isAvailable)
            {
                return new BookingResult { Success = false, ErrorMessage = "Table is not available at the selected time." };
            }

            // Apply coupon if provided
            Coupon? coupon = null;
            decimal discountAmount = 0;
            if (!string.IsNullOrWhiteSpace(request.CouponCode))
            {
                coupon = await _context.Coupons
                    .FirstOrDefaultAsync(c => c.Code == request.CouponCode && c.IsActive);

                if (coupon != null && coupon.EndDate >= DateTime.UtcNow && 
                    (coupon.MaxUsages == null || coupon.UsageCount < coupon.MaxUsages))
                {
                    if (coupon.Type == OfferType.Percentage && coupon.DiscountValue.HasValue)
                    {
                        discountAmount = (table.Branch.MinimumCharge ?? 0) * (coupon.DiscountValue.Value / 100);
                    }
                    else if (coupon.Type == OfferType.FixedAmount && coupon.DiscountValue.HasValue)
                    {
                        discountAmount = coupon.DiscountValue.Value;
                    }
                }
            }

            // Create booking
            var booking = new Booking
            {
                BookingReference = GenerateBookingReference(),
                BranchId = request.BranchId,
                TableId = request.TableId,
                UserId = request.UserId,
                GuestName = request.GuestName,
                GuestEmail = request.GuestEmail,
                GuestPhone = request.GuestPhone,
                PartySize = request.PartySize,
                BookingDate = request.BookingDate.Date,
                BookingTime = request.BookingTime,
                DurationMinutes = request.DurationMinutes,
                Status = BookingStatus.Pending,
                Occasion = request.Occasion,
                SpecialRequests = request.SpecialRequests,
                CouponId = coupon?.Id,
                DiscountAmount = discountAmount,
                CreatedAt = DateTime.UtcNow
            };

            // Handle deposit if required
            string? paymentIntentClientSecret = null;
            if (table.Branch.RequireDeposit && table.Branch.DepositAmount.HasValue)
            {
                var depositAmount = table.Branch.DepositAmount.Value - discountAmount;
                if (depositAmount > 0)
                {
                    var paymentResult = await _paymentService.CreatePaymentIntentAsync(
                        depositAmount,
                        "usd",
                        $"Deposit for booking at {table.Branch.Name}",
                        new Dictionary<string, string> { { "booking_reference", booking.BookingReference } });

                    if (!paymentResult.Success)
                    {
                        return new BookingResult 
                        { 
                            Success = false, 
                            ErrorMessage = $"Payment processing failed: {paymentResult.ErrorMessage}" 
                        };
                    }

                    booking.DepositAmount = depositAmount;
                    booking.StripePaymentIntentId = paymentResult.PaymentIntentId;
                    paymentIntentClientSecret = paymentResult.ClientSecret;
                }
            }

            _context.Bookings.Add(booking);
            
            // Update coupon usage
            if (coupon != null)
            {
                coupon.UsageCount++;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Generate QR code
            booking.QrCodeUrl = await GenerateQrCodeAsync(booking);
            await _context.SaveChangesAsync();

            // Send confirmation notification
            try
            {
                await _notificationService.SendBookingConfirmationAsync(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send booking confirmation notification");
            }

            _logger.LogInformation("Booking created successfully: {BookingReference}", booking.BookingReference);

            return new BookingResult 
            { 
                Success = true, 
                Booking = booking,
                PaymentIntentClientSecret = paymentIntentClientSecret
            };
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogWarning(ex, "Concurrency conflict during booking creation");
            return new BookingResult 
            { 
                Success = false, 
                ErrorMessage = "The selected time slot is no longer available. Please try again." 
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating booking");
            return new BookingResult { Success = false, ErrorMessage = "An error occurred while creating your booking." };
        }
    }

    public async Task<BookingResult> UpdateBookingAsync(int bookingId, BookingRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var booking = await _context.Bookings
                .Include(b => b.Branch)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                return new BookingResult { Success = false, ErrorMessage = "Booking not found." };
            }

            if (!await CanModifyBookingAsync(bookingId))
            {
                return new BookingResult { Success = false, ErrorMessage = "This booking can no longer be modified." };
            }

            // Check if table/time changed
            if (booking.TableId != request.TableId || 
                booking.BookingDate != request.BookingDate || 
                booking.BookingTime != request.BookingTime)
            {
                var isAvailable = await _availabilityService.IsTableAvailableAsync(
                    request.TableId, 
                    request.BookingDate, 
                    request.BookingTime, 
                    request.DurationMinutes,
                    bookingId);

                if (!isAvailable)
                {
                    return new BookingResult { Success = false, ErrorMessage = "New time slot is not available." };
                }
            }

            booking.TableId = request.TableId;
            booking.GuestName = request.GuestName;
            booking.GuestEmail = request.GuestEmail;
            booking.GuestPhone = request.GuestPhone;
            booking.PartySize = request.PartySize;
            booking.BookingDate = request.BookingDate.Date;
            booking.BookingTime = request.BookingTime;
            booking.DurationMinutes = request.DurationMinutes;
            booking.Occasion = request.Occasion;
            booking.SpecialRequests = request.SpecialRequests;
            booking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            try
            {
                await _notificationService.SendBookingModificationAsync(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send booking modification notification");
            }

            return new BookingResult { Success = true, Booking = booking };
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogWarning(ex, "Concurrency conflict during booking update");
            return new BookingResult 
            { 
                Success = false, 
                ErrorMessage = "The booking was modified by someone else. Please refresh and try again." 
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating booking");
            return new BookingResult { Success = false, ErrorMessage = "An error occurred while updating your booking." };
        }
    }

    public async Task<BookingResult> CancelBookingAsync(int bookingId, string? reason = null)
    {
        try
        {
            var booking = await _context.Bookings
                .Include(b => b.Branch)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                return new BookingResult { Success = false, ErrorMessage = "Booking not found." };
            }

            if (!await CanCancelBookingAsync(bookingId))
            {
                return new BookingResult 
                { 
                    Success = false, 
                    ErrorMessage = $"Bookings must be cancelled at least {booking.Branch.CancellationPolicyHours} hours before the reservation time." 
                };
            }

            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt = DateTime.UtcNow;
            booking.CancellationReason = reason;
            booking.UpdatedAt = DateTime.UtcNow;

            // Refund deposit if paid
            if (booking.DepositPaid && !string.IsNullOrEmpty(booking.StripePaymentIntentId))
            {
                var refundResult = await _paymentService.RefundPaymentAsync(booking.StripePaymentIntentId);
                if (!refundResult.Success)
                {
                    _logger.LogWarning("Failed to refund deposit for booking {BookingId}: {Error}", 
                        bookingId, refundResult.ErrorMessage);
                }
            }

            await _context.SaveChangesAsync();

            try
            {
                await _notificationService.SendBookingCancellationAsync(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send booking cancellation notification");
            }

            return new BookingResult { Success = true, Booking = booking };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking");
            return new BookingResult { Success = false, ErrorMessage = "An error occurred while cancelling your booking." };
        }
    }

    public async Task<BookingResult> ConfirmBookingAsync(int bookingId)
    {
        try
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
            {
                return new BookingResult { Success = false, ErrorMessage = "Booking not found." };
            }

            booking.Status = BookingStatus.Confirmed;
            booking.IsVerified = true;
            booking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new BookingResult { Success = true, Booking = booking };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming booking");
            return new BookingResult { Success = false, ErrorMessage = "An error occurred while confirming your booking." };
        }
    }

    public async Task<Booking?> GetBookingByIdAsync(int bookingId)
    {
        return await _context.Bookings
            .Include(b => b.Branch)
            .ThenInclude(b => b.Restaurant)
            .Include(b => b.Table)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == bookingId);
    }

    public async Task<Booking?> GetBookingByReferenceAsync(string reference)
    {
        return await _context.Bookings
            .Include(b => b.Branch)
            .ThenInclude(b => b.Restaurant)
            .Include(b => b.Table)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.BookingReference == reference);
    }

    public async Task<List<Booking>> GetBookingsForBranchAsync(int branchId, DateTime? date = null, BookingStatus? status = null)
    {
        var query = _context.Bookings
            .Include(b => b.Table)
            .Include(b => b.User)
            .Where(b => b.BranchId == branchId);

        if (date.HasValue)
        {
            query = query.Where(b => b.BookingDate.Date == date.Value.Date);
        }

        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }

        return await query
            .OrderByDescending(b => b.BookingDate)
            .ThenBy(b => b.BookingTime)
            .ToListAsync();
    }

    public async Task<List<Booking>> GetBookingsForUserAsync(string userId)
    {
        return await _context.Bookings
            .Include(b => b.Branch)
            .ThenInclude(b => b.Restaurant)
            .Include(b => b.Table)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BookingDate)
            .ThenBy(b => b.BookingTime)
            .ToListAsync();
    }

    public async Task<bool> CanCancelBookingAsync(int bookingId)
    {
        var booking = await _context.Bookings
            .Include(b => b.Branch)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null || booking.Status == BookingStatus.Cancelled)
        {
            return false;
        }

        var bookingDateTime = booking.BookingDate.Date.Add(booking.BookingTime);
        var cancellationDeadline = bookingDateTime.AddHours(-booking.Branch.CancellationPolicyHours);

        return DateTime.UtcNow < cancellationDeadline;
    }

    public async Task<bool> CanModifyBookingAsync(int bookingId)
    {
        return await CanCancelBookingAsync(bookingId);
    }

    public async Task<string> GenerateQrCodeAsync(Booking booking)
    {
        try
        {
            var qrContent = $"BOOKING:{booking.BookingReference}|{booking.GuestName}|{booking.BookingDate:yyyy-MM-dd}|{booking.BookingTime:hh\\:mm}";
            
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20);
            
            // Save QR code to file
            var fileName = $"qr_{booking.BookingReference}.png";
            var filePath = Path.Combine("wwwroot", "uploads", "qrcodes", fileName);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            await File.WriteAllBytesAsync(filePath, qrCodeBytes);
            
            return $"/uploads/qrcodes/{fileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code for booking {BookingReference}", booking.BookingReference);
            return string.Empty;
        }
    }

    private static string GenerateBookingReference()
    {
        var timestamp = DateTime.UtcNow.ToString("yyMMddHHmmss");
        var random = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"BR{timestamp}{random}";
    }
}
