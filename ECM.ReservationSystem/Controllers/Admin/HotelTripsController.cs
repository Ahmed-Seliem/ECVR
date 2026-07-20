using ECM.ReservationSystem.Models.DTOs.HotelTrips;
using ECM.ReservationSystem.Services.HotelTrips.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ECM.ReservationSystem.Controllers.Admin;

[Route("Admin/[controller]")]
[Authorize]
public class HotelTripsController : Controller
{
    private readonly IHotelTripService _hotelTripService;
    private readonly IHotelService _hotelService;

    public HotelTripsController(IHotelTripService hotelTripService, IHotelService hotelService)
    {
        _hotelTripService = hotelTripService;
        _hotelService = hotelService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? hotelId = null, int? year = null, int? month = null)
    {
        await PopulateHotelsAsync(hotelId);
        ViewBag.Years = Enumerable.Range(DateTime.Now.Year - 2, 5).ToList();
        ViewBag.SelectedYear = year;
        ViewBag.SelectedMonth = month;
        var trips = await _hotelTripService.GetAllAsync(hotelId, year, month);
        return View(trips);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HotelTripRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = GetModelStateErrors();
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _hotelTripService.CreateAsync(request);
            TempData["Success"] = "تم إضافة الرحلة بنجاح";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, HotelTripRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = GetModelStateErrors();
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var updated = await _hotelTripService.UpdateAsync(id, request);
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

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _hotelTripService.DeleteAsync(id);
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

    private async Task PopulateHotelsAsync(int? selectedId)
    {
        var hotels = await _hotelService.GetAllAsync();
        ViewBag.Hotels = new SelectList(hotels, nameof(HotelResponseDto.Id), nameof(HotelResponseDto.Name), selectedId);
    }

    private string GetModelStateErrors()
    {
        var errors = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct()
            .ToList();

        return errors.Count > 0 ? string.Join(" • ", errors) : "البيانات المدخلة غير صحيحة";
    }
}
