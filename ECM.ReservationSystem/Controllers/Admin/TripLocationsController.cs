using ECM.ReservationSystem.Models.DTOs.OneDayTrips;
using ECM.ReservationSystem.Services.OneDayTrips.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECM.ReservationSystem.Controllers.Admin;

[Route("Admin/[controller]")]
[Authorize]
public class TripLocationsController : Controller
{
    private readonly ITripLocationService _tripLocationService;

    public TripLocationsController(ITripLocationService tripLocationService)
    {
        _tripLocationService = tripLocationService;
    }

    // GET: Admin/TripLocations
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var locations = await _tripLocationService.GetAllAsync();
        return View(locations);
    }

    // POST: Admin/TripLocations/Create
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TripLocationRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = GetModelStateErrors();
            return RedirectToAction(nameof(Index));
        }

        await _tripLocationService.CreateAsync(request);
        TempData["Success"] = "تم إضافة المكان بنجاح";
        return RedirectToAction(nameof(Index));
    }

    // POST: Admin/TripLocations/Edit/5
    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TripLocationRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = GetModelStateErrors();
            return RedirectToAction(nameof(Index));
        }

        var updated = await _tripLocationService.UpdateAsync(id, request);
        if (!updated)
        {
            return NotFound();
        }

        TempData["Success"] = "تم تحديث المكان بنجاح";
        return RedirectToAction(nameof(Index));
    }

    // POST: Admin/TripLocations/Delete/5
    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _tripLocationService.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound();
            }

            TempData["Success"] = "تم حذف المكان بنجاح";
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
}
