using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantReservation.Web.Data;
using RestaurantReservation.Web.Models.Entities;
using RestaurantReservation.Web.Models.ViewModels;
using RestaurantReservation.Web.Services.Interfaces;

namespace RestaurantReservation.Web.Controllers;

public class BookingsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IBookingService _bookingService;
    private readonly IAvailabilityService _availabilityService;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(
        ApplicationDbContext context,
        IBookingService bookingService,
        IAvailabilityService availabilityService,
        ILogger<BookingsController> logger)
    {
        _context = context;
        _bookingService = bookingService;
        _availabilityService = availabilityService;
        _logger = logger;
    }

    // Step 1: Select date and party size
    [HttpGet]
    public async Task<IActionResult> Create(int branchId)
    {
        var branch = await _context.Branches
            .Include(b => b.Restaurant)
            .FirstOrDefaultAsync(b => b.Id == branchId && b.IsActive);

        if (branch == null)
        {
            return NotFound();
        }

        var viewModel = new BookingViewModel
        {
            BranchId = branchId,
            BranchName = branch.Name,
            RestaurantName = branch.Restaurant.Name,
            BookingDate = DateTime.Today.AddDays(1),
            PartySize = 2
        };

        return View(viewModel);
    }

    // Step 2: Select time slot
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectTime(BookingViewModel model)
    {
        var branch = await _context.Branches
            .Include(b => b.Restaurant)
            .FirstOrDefaultAsync(b => b.Id == model.BranchId && b.IsActive);

        if (branch == null)
        {
            return NotFound();
        }

        model.BranchName = branch.Name;
        model.RestaurantName = branch.Restaurant.Name;

        var timeSlots = await _availabilityService.GetAvailableTimeSlotsAsync(
            model.BranchId, model.BookingDate, model.PartySize);

        model.AvailableTimeSlots = timeSlots.Select(ts => new TimeSlotViewModel
        {
            Id = ts.Id,
            StartTime = ts.StartTime,
            EndTime = ts.EndTime,
            MealType = ts.MealType,
            IsAvailable = true
        }).ToList();

        return View(model);
    }

    // Step 3: Select table
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectTable(BookingViewModel model)
    {
        var branch = await _context.Branches
            .Include(b => b.Restaurant)
            .FirstOrDefaultAsync(b => b.Id == model.BranchId && b.IsActive);

        if (branch == null)
        {
            return NotFound();
        }

        if (!model.SelectedTime.HasValue)
        {
            ModelState.AddModelError("", "Please select a time slot");
            return View("SelectTime", model);
        }

        model.BranchName = branch.Name;
        model.RestaurantName = branch.Restaurant.Name;

        var tables = await _availabilityService.GetAvailableTablesAsync(
            model.BranchId, model.BookingDate, model.SelectedTime.Value, model.PartySize, model.PreferredLocation);

        model.AvailableTables = tables.Select(t => new TableViewModel
        {
            Id = t.Id,
            TableNumber = t.TableNumber,
            MinCapacity = t.MinCapacity,
            MaxCapacity = t.MaxCapacity,
            LocationType = t.LocationType,
            Description = t.Description,
            PhotoUrls = !string.IsNullOrEmpty(t.PhotosJson) 
                ? JsonSerializer.Deserialize<List<string>>(t.PhotosJson) ?? new List<string>()
                : new List<string>()
        }).ToList();

        return View(model);
    }

    // Step 4: Enter guest details
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuestDetails(BookingViewModel model)
    {
        var branch = await _context.Branches
            .Include(b => b.Restaurant)
            .FirstOrDefaultAsync(b => b.Id == model.BranchId && b.IsActive);

        if (branch == null)
        {
            return NotFound();
        }

        if (!model.SelectedTableId.HasValue || !model.SelectedTime.HasValue)
        {
            return RedirectToAction(nameof(Create), new { branchId = model.BranchId });
        }

        var table = await _context.Tables.FindAsync(model.SelectedTableId.Value);
        if (table == null)
        {
            return NotFound();
        }

        var detailsModel = new BookingDetailsViewModel
        {
            BranchId = model.BranchId,
            TableId = model.SelectedTableId.Value,
            PartySize = model.PartySize,
            BookingDate = model.BookingDate,
            BookingTime = model.SelectedTime.Value,
            BranchName = branch.Name,
            RestaurantName = branch.Restaurant.Name,
            TableNumber = table.TableNumber,
            TableLocation = table.LocationType,
            RequireDeposit = branch.RequireDeposit,
            DepositAmount = branch.DepositAmount
        };

        // Pre-fill user info if logged in
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                detailsModel.GuestName = $"{(user as ApplicationUser)?.FirstName} {(user as ApplicationUser)?.LastName}".Trim();
                detailsModel.GuestEmail = user.Email ?? "";
                detailsModel.GuestPhone = user.PhoneNumber;
            }
        }

        return View(detailsModel);
    }

    // Step 5: Confirm booking
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(BookingDetailsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var branch = await _context.Branches
                .Include(b => b.Restaurant)
                .FirstOrDefaultAsync(b => b.Id == model.BranchId);
            
            if (branch != null)
            {
                model.BranchName = branch.Name;
                model.RestaurantName = branch.Restaurant.Name;
                model.RequireDeposit = branch.RequireDeposit;
                model.DepositAmount = branch.DepositAmount;
            }
            
            return View("GuestDetails", model);
        }

        var request = new BookingRequest
        {
            BranchId = model.BranchId,
            TableId = model.TableId,
            UserId = User.Identity?.IsAuthenticated == true 
                ? User.FindFirstValue(ClaimTypes.NameIdentifier) 
                : null,
            GuestName = model.GuestName,
            GuestEmail = model.GuestEmail,
            GuestPhone = model.GuestPhone,
            PartySize = model.PartySize,
            BookingDate = model.BookingDate,
            BookingTime = model.BookingTime,
            DurationMinutes = model.DurationMinutes,
            Occasion = model.Occasion,
            SpecialRequests = model.SpecialRequests,
            CouponCode = model.CouponCode
        };

        var result = await _bookingService.CreateBookingAsync(request);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.ErrorMessage ?? "Failed to create booking");
            return View("GuestDetails", model);
        }

        if (!string.IsNullOrEmpty(result.PaymentIntentClientSecret))
        {
            // Redirect to payment page
            return RedirectToAction(nameof(Payment), new 
            { 
                bookingId = result.Booking!.Id,
                clientSecret = result.PaymentIntentClientSecret 
            });
        }

        return RedirectToAction(nameof(Confirmation), new { id = result.Booking!.Id });
    }

    // Payment page
    [HttpGet]
    public async Task<IActionResult> Payment(int bookingId, string clientSecret)
    {
        var booking = await _bookingService.GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return NotFound();
        }

        ViewBag.ClientSecret = clientSecret;
        ViewBag.StripePublishableKey = _context.Database.GetConnectionString(); // Get from config
        
        return View(booking);
    }

    // Booking confirmation
    [HttpGet]
    public async Task<IActionResult> Confirmation(int id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);
        if (booking == null)
        {
            return NotFound();
        }

        var viewModel = new BookingConfirmationViewModel
        {
            BookingReference = booking.BookingReference,
            GuestName = booking.GuestName,
            BranchName = booking.Branch.Name,
            RestaurantName = booking.Branch.Restaurant.Name,
            Address = booking.Branch.Address,
            BookingDate = booking.BookingDate,
            BookingTime = booking.BookingTime,
            PartySize = booking.PartySize,
            TableNumber = booking.Table.TableNumber,
            QrCodeUrl = booking.QrCodeUrl,
            Status = booking.Status,
            CanCancel = await _bookingService.CanCancelBookingAsync(id),
            CanModify = await _bookingService.CanModifyBookingAsync(id)
        };

        return View(viewModel);
    }

    // View booking by reference
    [HttpGet]
    [Route("Bookings/View/{reference?}")]
    public async Task<IActionResult> ViewBooking(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return View("Lookup");
        }

        var booking = await _bookingService.GetBookingByReferenceAsync(reference);
        if (booking == null)
        {
            ViewBag.Error = "Booking not found";
            return View("Lookup");
        }

        var viewModel = new BookingConfirmationViewModel
        {
            BookingReference = booking.BookingReference,
            GuestName = booking.GuestName,
            BranchName = booking.Branch.Name,
            RestaurantName = booking.Branch.Restaurant.Name,
            Address = booking.Branch.Address,
            BookingDate = booking.BookingDate,
            BookingTime = booking.BookingTime,
            PartySize = booking.PartySize,
            TableNumber = booking.Table.TableNumber,
            QrCodeUrl = booking.QrCodeUrl,
            Status = booking.Status,
            CanCancel = await _bookingService.CanCancelBookingAsync(booking.Id),
            CanModify = await _bookingService.CanModifyBookingAsync(booking.Id)
        };

        return View("Confirmation", viewModel);
    }

    // Lookup booking
    [HttpGet]
    public IActionResult Lookup()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Lookup(string reference)
    {
        return RedirectToAction(nameof(ViewBooking), new { reference });
    }

    // Cancel booking
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason)
    {
        var result = await _bookingService.CancelBookingAsync(id, reason);
        
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] = "Your booking has been cancelled successfully.";
        }

        return RedirectToAction(nameof(ViewBooking), new { reference = result.Booking?.BookingReference });
    }

    // My bookings (for logged-in users)
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> MyBookings()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var bookings = await _bookingService.GetBookingsForUserAsync(userId);
        
        var viewModels = bookings.Select(b => new BookingConfirmationViewModel
        {
            BookingReference = b.BookingReference,
            GuestName = b.GuestName,
            BranchName = b.Branch.Name,
            RestaurantName = b.Branch.Restaurant.Name,
            Address = b.Branch.Address,
            BookingDate = b.BookingDate,
            BookingTime = b.BookingTime,
            PartySize = b.PartySize,
            TableNumber = b.Table.TableNumber,
            QrCodeUrl = b.QrCodeUrl,
            Status = b.Status
        }).ToList();

        return View(viewModels);
    }

    // API endpoints for AJAX
    [HttpGet]
    public async Task<IActionResult> GetAvailableTimeSlots(int branchId, DateTime date, int partySize)
    {
        var timeSlots = await _availabilityService.GetAvailableTimeSlotsAsync(branchId, date, partySize);
        
        var result = timeSlots.Select(ts => new
        {
            id = ts.Id,
            startTime = ts.StartTime.ToString(@"hh\:mm"),
            endTime = ts.EndTime.ToString(@"hh\:mm"),
            mealType = ts.MealType.ToString()
        });

        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailableTables(int branchId, DateTime date, string time, int partySize, TableLocationType? locationType = null)
    {
        if (!TimeSpan.TryParse(time, out var timeSpan))
        {
            return BadRequest("Invalid time format");
        }

        var tables = await _availabilityService.GetAvailableTablesAsync(branchId, date, timeSpan, partySize, locationType);
        
        var result = tables.Select(t => new
        {
            id = t.Id,
            tableNumber = t.TableNumber,
            minCapacity = t.MinCapacity,
            maxCapacity = t.MaxCapacity,
            locationType = t.LocationType.ToString(),
            description = t.Description
        });

        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailabilityCalendar(int branchId, int partySize, int months = 2)
    {
        var startDate = DateTime.Today;
        var endDate = startDate.AddMonths(months);

        var availability = await _availabilityService.GetAvailabilityCalendarAsync(branchId, startDate, endDate, partySize);

        var result = availability.Select(kvp => new
        {
            date = kvp.Key,
            availableSlots = kvp.Value,
            hasAvailability = kvp.Value > 0
        });

        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> ValidateCoupon(string code, int branchId)
    {
        var branch = await _context.Branches.FindAsync(branchId);
        if (branch == null)
        {
            return Json(new { valid = false, message = "Invalid branch" });
        }

        var coupon = await _context.Coupons
            .FirstOrDefaultAsync(c => c.Code == code && c.IsActive);

        if (coupon == null)
        {
            return Json(new { valid = false, message = "Invalid coupon code" });
        }

        if (coupon.EndDate < DateTime.UtcNow)
        {
            return Json(new { valid = false, message = "Coupon has expired" });
        }

        if (coupon.MaxUsages.HasValue && coupon.UsageCount >= coupon.MaxUsages)
        {
            return Json(new { valid = false, message = "Coupon usage limit reached" });
        }

        if (coupon.RestaurantId.HasValue && coupon.RestaurantId != branch.RestaurantId)
        {
            return Json(new { valid = false, message = "Coupon not valid for this restaurant" });
        }

        if (coupon.BranchId.HasValue && coupon.BranchId != branchId)
        {
            return Json(new { valid = false, message = "Coupon not valid for this branch" });
        }

        return Json(new
        {
            valid = true,
            description = coupon.Description,
            discountType = coupon.Type.ToString(),
            discountValue = coupon.DiscountValue
        });
    }
}
