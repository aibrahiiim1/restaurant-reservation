using System.Security.Claims;
using System.Text.Json;
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
public class TablesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<TablesController> _logger;

    public TablesController(
        ApplicationDbContext context,
        IFileUploadService fileUploadService,
        ILogger<TablesController> logger)
    {
        _context = context;
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int? branchId)
    {
        // Get list of branches for filter dropdown
        var branches = await _context.Branches
            .Include(b => b.Restaurant)
            .OrderBy(b => b.Restaurant.Name)
            .ThenBy(b => b.Name)
            .ToListAsync();
        
        ViewBag.Branches = branches;
        ViewBag.BranchId = branchId;

        if (branchId.HasValue && branchId.Value > 0)
        {
            var branch = branches.FirstOrDefault(b => b.Id == branchId.Value);
            if (branch == null)
            {
                return NotFound();
            }

            var tables = await _context.Tables
                .Where(t => t.BranchId == branchId.Value)
                .OrderBy(t => t.TableNumber)
                .ToListAsync();

            ViewBag.BranchName = $"{branch.Restaurant.Name} - {branch.Name}";
            return View(tables);
        }
        else
        {
            // Show all tables across all branches
            var tables = await _context.Tables
                .Include(t => t.Branch)
                .ThenInclude(b => b.Restaurant)
                .OrderBy(t => t.Branch.Restaurant.Name)
                .ThenBy(t => t.Branch.Name)
                .ThenBy(t => t.TableNumber)
                .ToListAsync();

            ViewBag.BranchName = "All Branches";
            return View(tables);
        }
    }

    public async Task<IActionResult> Create(int branchId)
    {
        var branch = await _context.Branches
            .Include(b => b.Restaurant)
            .FirstOrDefaultAsync(b => b.Id == branchId);

        if (branch == null)
        {
            return NotFound();
        }

        var viewModel = new TableCreateViewModel
        {
            BranchId = branchId,
            BranchName = $"{branch.Restaurant.Name} - {branch.Name}"
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TableCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var branch = await _context.Branches
                .Include(b => b.Restaurant)
                .FirstOrDefaultAsync(b => b.Id == model.BranchId);
            model.BranchName = branch != null ? $"{branch.Restaurant.Name} - {branch.Name}" : "";
            return View(model);
        }

        // Check for duplicate table number
        var exists = await _context.Tables
            .AnyAsync(t => t.BranchId == model.BranchId && t.TableNumber == model.TableNumber);

        if (exists)
        {
            ModelState.AddModelError("TableNumber", "A table with this number already exists in this branch");
            return View(model);
        }

        var table = new Table
        {
            BranchId = model.BranchId,
            TableNumber = model.TableNumber,
            MinCapacity = model.MinCapacity,
            MaxCapacity = model.MaxCapacity,
            LocationType = model.LocationType,
            Description = model.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        if (model.LayoutX.HasValue && model.LayoutY.HasValue)
        {
            table.LayoutJson = JsonSerializer.Serialize(new
            {
                x = model.LayoutX,
                y = model.LayoutY,
                width = model.LayoutWidth ?? 80,
                height = model.LayoutHeight ?? 80
            });
        }

        if (model.Photos != null && model.Photos.Any())
        {
            var photoUrls = await _fileUploadService.UploadFilesAsync(model.Photos, "tables");
            table.PhotosJson = JsonSerializer.Serialize(photoUrls);
        }

        _context.Tables.Add(table);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Table created: {TableNumber} in branch {BranchId}", table.TableNumber, table.BranchId);
        TempData["Success"] = "Table created successfully";

        return RedirectToAction(nameof(Index), new { branchId = model.BranchId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var table = await _context.Tables
            .Include(t => t.Branch)
            .ThenInclude(b => b.Restaurant)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (table == null)
        {
            return NotFound();
        }

        var photoUrls = new List<string>();
        if (!string.IsNullOrEmpty(table.PhotosJson))
        {
            try
            {
                photoUrls = JsonSerializer.Deserialize<List<string>>(table.PhotosJson) ?? new List<string>();
            }
            catch { }
        }

        int? layoutX = null, layoutY = null, layoutWidth = null, layoutHeight = null;
        if (!string.IsNullOrEmpty(table.LayoutJson))
        {
            try
            {
                var layout = JsonSerializer.Deserialize<JsonElement>(table.LayoutJson);
                layoutX = layout.GetProperty("x").GetInt32();
                layoutY = layout.GetProperty("y").GetInt32();
                layoutWidth = layout.TryGetProperty("width", out var w) ? w.GetInt32() : 80;
                layoutHeight = layout.TryGetProperty("height", out var h) ? h.GetInt32() : 80;
            }
            catch { }
        }

        var viewModel = new TableEditViewModel
        {
            Id = table.Id,
            BranchId = table.BranchId,
            TableNumber = table.TableNumber,
            MinCapacity = table.MinCapacity,
            MaxCapacity = table.MaxCapacity,
            LocationType = table.LocationType,
            Description = table.Description,
            CurrentPhotoUrls = photoUrls,
            IsActive = table.IsActive,
            BranchName = $"{table.Branch.Restaurant.Name} - {table.Branch.Name}",
            LayoutX = layoutX,
            LayoutY = layoutY,
            LayoutWidth = layoutWidth,
            LayoutHeight = layoutHeight
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TableEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var table = await _context.Tables.FindAsync(model.Id);
        if (table == null)
        {
            return NotFound();
        }

        // Check for duplicate table number (excluding current table)
        var exists = await _context.Tables
            .AnyAsync(t => t.BranchId == model.BranchId && t.TableNumber == model.TableNumber && t.Id != model.Id);

        if (exists)
        {
            ModelState.AddModelError("TableNumber", "A table with this number already exists in this branch");
            return View(model);
        }

        table.TableNumber = model.TableNumber;
        table.MinCapacity = model.MinCapacity;
        table.MaxCapacity = model.MaxCapacity;
        table.LocationType = model.LocationType;
        table.Description = model.Description;
        table.IsActive = model.IsActive;
        table.UpdatedAt = DateTime.UtcNow;

        if (model.LayoutX.HasValue && model.LayoutY.HasValue)
        {
            table.LayoutJson = JsonSerializer.Serialize(new
            {
                x = model.LayoutX,
                y = model.LayoutY,
                width = model.LayoutWidth ?? 80,
                height = model.LayoutHeight ?? 80
            });
        }

        if (model.Photos != null && model.Photos.Any())
        {
            var newPhotoUrls = await _fileUploadService.UploadFilesAsync(model.Photos, "tables");
            var existingPhotos = new List<string>();
            if (!string.IsNullOrEmpty(table.PhotosJson))
            {
                existingPhotos = JsonSerializer.Deserialize<List<string>>(table.PhotosJson) ?? new List<string>();
            }
            existingPhotos.AddRange(newPhotoUrls);
            table.PhotosJson = JsonSerializer.Serialize(existingPhotos);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Table updated: {TableNumber}", table.TableNumber);
        TempData["Success"] = "Table updated successfully";

        return RedirectToAction(nameof(Index), new { branchId = table.BranchId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var table = await _context.Tables
            .Include(t => t.Bookings)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (table == null)
        {
            return NotFound();
        }

        var hasActiveBookings = table.Bookings.Any(b => 
            b.Status == BookingStatus.Pending || 
            b.Status == BookingStatus.Confirmed);

        if (hasActiveBookings)
        {
            TempData["Error"] = "Cannot delete table with active bookings";
            return RedirectToAction(nameof(Index), new { branchId = table.BranchId });
        }

        // Delete photos
        if (!string.IsNullOrEmpty(table.PhotosJson))
        {
            var photos = JsonSerializer.Deserialize<List<string>>(table.PhotosJson);
            if (photos != null)
            {
                foreach (var photo in photos)
                {
                    await _fileUploadService.DeleteFileAsync(photo);
                }
            }
        }

        var branchId = table.BranchId;
        _context.Tables.Remove(table);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Table deleted: {TableNumber}", table.TableNumber);
        TempData["Success"] = "Table deleted successfully";

        return RedirectToAction(nameof(Index), new { branchId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var table = await _context.Tables.FindAsync(id);
        if (table == null)
        {
            return NotFound();
        }

        table.IsActive = !table.IsActive;
        table.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Table {(table.IsActive ? "activated" : "deactivated")} successfully";

        return RedirectToAction(nameof(Index), new { branchId = table.BranchId });
    }

    public async Task<IActionResult> Layout(int branchId)
    {
        var branch = await _context.Branches
            .Include(b => b.Restaurant)
            .FirstOrDefaultAsync(b => b.Id == branchId);

        if (branch == null)
        {
            return NotFound();
        }

        var tables = await _context.Tables
            .Where(t => t.BranchId == branchId && t.IsActive)
            .ToListAsync();

        ViewBag.BranchName = $"{branch.Restaurant.Name} - {branch.Name}";
        ViewBag.BranchId = branchId;

        return View(tables);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateLayout([FromBody] List<TableLayoutUpdate> updates)
    {
        foreach (var update in updates)
        {
            var table = await _context.Tables.FindAsync(update.Id);
            if (table != null)
            {
                table.LayoutJson = JsonSerializer.Serialize(new
                {
                    x = update.X,
                    y = update.Y,
                    width = update.Width,
                    height = update.Height
                });
            }
        }

        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }
}

public class TableLayoutUpdate
{
    public int Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
