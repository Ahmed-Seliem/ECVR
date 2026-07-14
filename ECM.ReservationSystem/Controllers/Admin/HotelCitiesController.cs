using ECM.ReservationSystem.Models.DTOs.HotelTrips;
using ECM.ReservationSystem.Services.HotelTrips.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECM.ReservationSystem.Controllers.Admin;

[Route("Admin/[controller]")]
[Authorize]
public class HotelCitiesController : Controller
{
    private readonly IHotelCityService _hotelCityService;

    public HotelCitiesController(IHotelCityService hotelCityService)
    {
        _hotelCityService = hotelCityService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var cities = await _hotelCityService.GetAllAsync();
        return View(cities);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HotelCityRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = GetModelStateErrors();
            return RedirectToAction(nameof(Index));
        }

        await _hotelCityService.CreateAsync(request);
        TempData["Success"] = "تم إضافة المدينة بنجاح";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, HotelCityRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = GetModelStateErrors();
            return RedirectToAction(nameof(Index));
        }

        var updated = await _hotelCityService.UpdateAsync(id, request);
        if (!updated)
        {
            return NotFound();
        }

        TempData["Success"] = "تم تحديث المدينة بنجاح";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _hotelCityService.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound();
            }

            TempData["Success"] = "تم حذف المدينة بنجاح";
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

        return errors.Count > 0 ? string.Join(" • ", errors) : "البيانات المدخلة غير صحيحة";
    }
}
