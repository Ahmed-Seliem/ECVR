using ECM.ReservationSystem.Models.DTOs.OneDayTrips;
using ECM.ReservationSystem.Services.OneDayTrips.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ECM.ReservationSystem.Controllers.Admin;

[Route("Admin/[controller]")]
[Authorize]
public class TripBookingsController : Controller
{
    private readonly ITripBookingService _tripBookingService;
    private readonly ITripService _tripService;

    public TripBookingsController(ITripBookingService tripBookingService, ITripService tripService)
    {
        _tripBookingService = tripBookingService;
        _tripService = tripService;
    }

    // GET: Admin/TripBookings
    [HttpGet("")]
    public async Task<IActionResult> Index(int? tripId = null)
    {
        await PopulateTripsAsync(tripId);
        var bookings = await _tripBookingService.GetAllAsync(tripId);
        return View(bookings);
    }

    // GET: Admin/TripBookings/Details/5
    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var booking = await _tripBookingService.GetByIdAsync(id);
        if (booking is null)
        {
            return NotFound();
        }

        return View(booking);
    }

    // POST: Admin/TripBookings/Cancel/5
    [HttpPost("Cancel/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var cancelled = await _tripBookingService.CancelAsync(id);
        if (!cancelled)
        {
            return NotFound();
        }

        TempData["Success"] = "تم إلغاء الحجز بنجاح";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateTripsAsync(int? selectedId)
    {
        var trips = await _tripService.GetAllAsync();
        ViewBag.Trips = new SelectList(
            trips.Select(t => new
            {
                t.Id,
                Display = $"{t.LocationName} - {t.TripDate:dd/MM/yyyy}"
            }),
            "Id",
            "Display",
            selectedId);
    }
}
