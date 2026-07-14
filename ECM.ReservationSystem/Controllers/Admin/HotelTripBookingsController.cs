using ECM.ReservationSystem.Services.HotelTrips.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ECM.ReservationSystem.Controllers.Admin;

[Route("Admin/[controller]")]
[Authorize]
public class HotelTripBookingsController : Controller
{
    private readonly IHotelTripBookingService _bookingService;
    private readonly IHotelTripService _hotelTripService;

    public HotelTripBookingsController(IHotelTripBookingService bookingService, IHotelTripService hotelTripService)
    {
        _bookingService = bookingService;
        _hotelTripService = hotelTripService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? hotelTripId = null, Domain.Entities.HotelTrips.HotelBookingType? bookingType = null)
    {
        await PopulateTripsAsync(hotelTripId);
        ViewBag.SelectedBookingType = bookingType;
        var bookings = await _bookingService.GetAllAsync(hotelTripId, bookingType);
        return View(bookings);
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var booking = await _bookingService.GetByIdAsync(id);
        if (booking is null)
        {
            return NotFound();
        }

        return View(booking);
    }

    [HttpPost("Cancel/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var cancelled = await _bookingService.CancelAsync(id);
        if (!cancelled)
        {
            return NotFound();
        }

        TempData["Success"] = "تم إلغاء الحجز بنجاح";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("UpdateStatus/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, Domain.Entities.HotelTrips.HotelBookingStatus status)
    {
        var updated = await _bookingService.SetStatusAsync(id, status);
        if (!updated)
        {
            return NotFound();
        }

        TempData["Success"] = "تم تحديث حالة الحجز بنجاح";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateTripsAsync(int? selectedId)
    {
        var trips = await _hotelTripService.GetAllAsync();
        ViewBag.Trips = new SelectList(
            trips.Select(t => new
            {
                t.Id,
                Display = $"{t.HotelName} - {t.StartDate:dd/MM/yyyy} إلى {t.EndDate:dd/MM/yyyy}"
            }),
            "Id",
            "Display",
            selectedId);
    }
}
