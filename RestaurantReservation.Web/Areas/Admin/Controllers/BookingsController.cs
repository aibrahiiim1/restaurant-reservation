using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantReservation.Web.Data;
using RestaurantReservation.Web.Models.Entities;
using RestaurantReservation.Web.Models.ViewModels;
using RestaurantReservation.Web.Services.Interfaces;

namespace RestaurantReservation.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin,RestaurantManager,BranchManager")]
public class BookingsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IBookingService _bookingService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(
        ApplicationDbContext context,
        IBookingService bookingService,
        INotificationService notificationService,
        ILogger<BookingsController> logger)
    {
        _context = context;
        _bookingService = bookingService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int? branchId, DateTime? date, BookingStatus? status)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _context.Users.OfType<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == userId);

        IQueryable<Booking> query = _context.Bookings
            .Include(b => b.Branch)
            .Include(b => b.Table);

        if (User.IsInRole("RestaurantManager") && user?.RestaurantId.HasValue == true)
        {
            query = query.Where(b => b.Branch.RestaurantId == user.RestaurantId);
        }
        else if (User.IsInRole("BranchManager") && user?.BranchId.HasValue == true)
        {
            query = query.Where(b => b.BranchId == user.BranchId);
        }
        else if (branchId.HasValue)
        {
            query = query.Where(b => b.BranchId == branchId);
        }

        if (date.HasValue)
        {
            query = query.Where(b => b.BookingDate.Date == date.Value.Date);
        }

        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status);
        }

        var bookings = await query
            .OrderByDescending(b => b.BookingDate)
            .ThenBy(b => b.BookingTime)
            .Take(100)
            .Select(b => new BookingListViewModel
            {
                Id = b.Id,
                BookingReference = b.BookingReference,
                GuestName = b.GuestName,
                GuestEmail = b.GuestEmail,
                PartySize = b.PartySize,
                BookingDate = b.BookingDate,
                BookingTime = b.BookingTime,
                TableNumber = b.Table.TableNumber,
                Status = b.Status,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync();

        // Get branches for filter
        IQueryable<Branch> branchQuery = _context.Branches.Include(b => b.Restaurant);
        if (User.IsInRole("RestaurantManager") && user?.RestaurantId.HasValue == true)
        {
            branchQuery = branchQuery.Where(b => b.RestaurantId == user.RestaurantId);
        }
        else if (User.IsInRole("BranchManager") && user?.BranchId.HasValue == true)
        {
            branchQuery = branchQuery.Where(b => b.Id == user.BranchId);
        }

        ViewBag.Branches = new SelectList(
            await branchQuery.Select(b => new { b.Id, Name = b.Restaurant.Name + " - " + b.Name }).ToListAsync(),
            "Id",
            "Name",
            branchId);

        ViewBag.Statuses = new SelectList(
            Enum.GetValues<BookingStatus>().Select(s => new { Value = s, Text = s.ToString() }),
            "Value",
            "Text",
            status);

        ViewBag.CurrentDate = date;
        ViewBag.CurrentBranchId = branchId;
        ViewBag.CurrentStatus = status;

        return View(bookings);
    }

    public async Task<IActionResult> Details(int id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);
        if (booking == null)
        {
            return NotFound();
        }

        return View(booking);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var booking = await _context.Bookings
            .Include(b => b.Branch)
            .Include(b => b.Table)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
        {
            return NotFound();
        }

        var viewModel = new BookingAdminEditViewModel
        {
            Id = booking.Id,
            BookingReference = booking.BookingReference,
            BranchId = booking.BranchId,
            TableId = booking.TableId,
            GuestName = booking.GuestName,
            GuestEmail = booking.GuestEmail,
            GuestPhone = booking.GuestPhone,
            PartySize = booking.PartySize,
            BookingDate = booking.BookingDate,
            BookingTime = booking.BookingTime,
            DurationMinutes = booking.DurationMinutes,
            Status = booking.Status,
            Occasion = booking.Occasion,
            SpecialRequests = booking.SpecialRequests,
            TableList = new SelectList(
                await _context.Tables.Where(t => t.BranchId == booking.BranchId && t.IsActive).ToListAsync(),
                "Id",
                "TableNumber",
                booking.TableId),
            StatusList = new SelectList(
                Enum.GetValues<BookingStatus>().Select(s => new { Value = s, Text = s.ToString() }),
                "Value",
                "Text",
                booking.Status)
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BookingAdminEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.TableList = new SelectList(
                await _context.Tables.Where(t => t.BranchId == model.BranchId && t.IsActive).ToListAsync(),
                "Id",
                "TableNumber",
                model.TableId);
            model.StatusList = new SelectList(
                Enum.GetValues<BookingStatus>().Select(s => new { Value = s, Text = s.ToString() }),
                "Value",
                "Text",
                model.Status);
            return View(model);
        }

        var booking = await _context.Bookings.FindAsync(model.Id);
        if (booking == null)
        {
            return NotFound();
        }

        booking.TableId = model.TableId;
        booking.GuestName = model.GuestName;
        booking.GuestEmail = model.GuestEmail;
        booking.GuestPhone = model.GuestPhone;
        booking.PartySize = model.PartySize;
        booking.BookingDate = model.BookingDate;
        booking.BookingTime = model.BookingTime;
        booking.DurationMinutes = model.DurationMinutes;
        booking.Status = model.Status;
        booking.Occasion = model.Occasion;
        booking.SpecialRequests = model.SpecialRequests;
        booking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Booking updated by admin: {BookingReference}", booking.BookingReference);
        TempData["Success"] = "Booking updated successfully";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, BookingStatus status)
    {
        var booking = await _context.Bookings
            .Include(b => b.Branch)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
        {
            return NotFound();
        }

        var oldStatus = booking.Status;
        booking.Status = status;
        booking.UpdatedAt = DateTime.UtcNow;

        if (status == BookingStatus.Cancelled)
        {
            booking.CancelledAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Booking status changed: {BookingReference} from {OldStatus} to {NewStatus}",
            booking.BookingReference, oldStatus, status);

        // Notify guest about status change
        if (status == BookingStatus.Confirmed)
        {
            await _notificationService.SendBookingConfirmationAsync(booking);
        }
        else if (status == BookingStatus.Cancelled)
        {
            await _notificationService.SendBookingCancellationAsync(booking);
        }

        TempData["Success"] = $"Booking status updated to {status}";

        return RedirectToAction(nameof(Index));
    }

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
            TempData["Success"] = "Booking cancelled successfully";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        var result = await _bookingService.ConfirmBookingAsync(id);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] = "Booking confirmed successfully";
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Calendar(int? branchId, DateTime? date)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _context.Users.OfType<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == userId);

        var viewDate = date ?? DateTime.Today;

        IQueryable<Branch> branchQuery = _context.Branches.Include(b => b.Restaurant);
        if (User.IsInRole("RestaurantManager") && user?.RestaurantId.HasValue == true)
        {
            branchQuery = branchQuery.Where(b => b.RestaurantId == user.RestaurantId);
        }
        else if (User.IsInRole("BranchManager") && user?.BranchId.HasValue == true)
        {
            branchQuery = branchQuery.Where(b => b.Id == user.BranchId);
        }

        ViewBag.Branches = new SelectList(
            await branchQuery.Select(b => new { b.Id, Name = b.Restaurant.Name + " - " + b.Name }).ToListAsync(),
            "Id",
            "Name",
            branchId);

        ViewBag.CurrentDate = viewDate;
        ViewBag.CurrentBranchId = branchId;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetCalendarEvents(int branchId, DateTime start, DateTime end)
    {
        var bookings = await _context.Bookings
            .Include(b => b.Table)
            .Where(b => b.BranchId == branchId)
            .Where(b => b.BookingDate >= start && b.BookingDate <= end)
            .Where(b => b.Status != BookingStatus.Cancelled)
            .ToListAsync();

        var events = bookings.Select(b => new
        {
            id = b.Id,
            title = $"{b.GuestName} ({b.PartySize}) - {b.Table.TableNumber}",
            start = b.BookingDate.Add(b.BookingTime).ToString("yyyy-MM-ddTHH:mm:ss"),
            end = b.BookingDate.Add(b.BookingTime).AddMinutes(b.DurationMinutes).ToString("yyyy-MM-ddTHH:mm:ss"),
            color = GetStatusColor(b.Status),
            extendedProps = new
            {
                bookingReference = b.BookingReference,
                guestName = b.GuestName,
                guestEmail = b.GuestEmail,
                partySize = b.PartySize,
                tableNumber = b.Table.TableNumber,
                status = b.Status.ToString()
            }
        });

        return Json(events);
    }

    private static string GetStatusColor(BookingStatus status)
    {
        return status switch
        {
            BookingStatus.Pending => "#ffc107",
            BookingStatus.Confirmed => "#28a745",
            BookingStatus.Completed => "#6c757d",
            BookingStatus.NoShow => "#dc3545",
            _ => "#17a2b8"
        };
    }
}
