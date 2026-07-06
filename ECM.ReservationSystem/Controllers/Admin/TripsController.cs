using ECM.ReservationSystem.Models.DTOs.OneDayTrips;
using ECM.ReservationSystem.Services.OneDayTrips.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ECM.ReservationSystem.Controllers.Admin;

[Route("Admin/[controller]")]
[Authorize]
public class TripsController : Controller
{
    private readonly ITripService _tripService;
    private readonly ITripLocationService _tripLocationService;

    public TripsController(ITripService tripService, ITripLocationService tripLocationService)
    {
        _tripService = tripService;
        _tripLocationService = tripLocationService;
    }

    // GET: Admin/Trips
    [HttpGet("")]
    public async Task<IActionResult> Index(int? locationId = null)
    {
        await PopulateLocationsAsync(locationId);
        var trips = await _tripService.GetAllAsync(locationId);
        return View(trips);
    }

    // POST: Admin/Trips/Create
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TripRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = GetModelStateErrors();
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _tripService.CreateAsync(request);
            TempData["Success"] = "تم إضافة الرحلة بنجاح";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: Admin/Trips/Edit/5
    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TripRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = GetModelStateErrors();
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var updated = await _tripService.UpdateAsync(id, request);
            if (!updated)
            {
                return NotFound();
            }

            TempData["Success"] = "تم تحديث الرحلة بنجاح";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: Admin/Trips/Delete/5
    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _tripService.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound();
            }

            TempData["Success"] = "تم حذف الرحلة بنجاح";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private string GetModelStateErrors()
    {
        var errors = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct()
            .ToList();

        return errors.Count > 0
            ? string.Join(" • ", errors)
            : "البيانات المدخلة غير صحيحة";
    }

    private async Task PopulateLocationsAsync(int? selectedId)
    {
        var locations = await _tripLocationService.GetAllAsync();
        ViewBag.Locations = new SelectList(
            locations,
            nameof(TripLocationResponseDto.Id),
            nameof(TripLocationResponseDto.Name),
            selectedId);
    }
}
