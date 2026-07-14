using ECM.ReservationSystem.Models.DTOs.HotelTrips;
using ECM.ReservationSystem.Services.HotelTrips.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ECM.ReservationSystem.Controllers.Admin;

[Route("Admin/[controller]")]
[Authorize]
public class HotelsController : Controller
{
    private readonly IHotelService _hotelService;
    private readonly IHotelCityService _hotelCityService;

    public HotelsController(IHotelService hotelService, IHotelCityService hotelCityService)
    {
        _hotelService = hotelService;
        _hotelCityService = hotelCityService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? cityId = null)
    {
        await PopulateCitiesAsync(cityId);
        var hotels = await _hotelService.GetAllAsync(cityId);
        return View(hotels);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HotelRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = GetModelStateErrors();
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _hotelService.CreateAsync(request);
            TempData["Success"] = "تم إضافة الفندق بنجاح";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, HotelRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = GetModelStateErrors();
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var updated = await _hotelService.UpdateAsync(id, request);
            if (!updated)
            {
                return NotFound();
            }

            TempData["Success"] = "تم تحديث الفندق بنجاح";
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
            var deleted = await _hotelService.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound();
            }

            TempData["Success"] = "تم حذف الفندق بنجاح";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCitiesAsync(int? selectedId)
    {
        var cities = await _hotelCityService.GetAllAsync();
        ViewBag.Cities = new SelectList(cities, nameof(HotelCityResponseDto.Id), nameof(HotelCityResponseDto.Name), selectedId);
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
