using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities.OneDayTrips;
using ECM.ReservationSystem.Domain.OneDayTrips;
using ECM.ReservationSystem.Models.DTOs.OneDayTrips;
using ECM.ReservationSystem.Services.OneDayTrips.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ECM.ReservationSystem.Controllers.API;

[Route("api/OneDayTrip")]
[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class OneDayTripApiController : ControllerBase
{
    private static readonly CultureInfo ArabicCulture = CultureInfo.GetCultureInfo("ar-EG");

    private readonly ApplicationDbContext _context;
    private readonly ITripBookingService _tripBookingService;

    public OneDayTripApiController(
        ApplicationDbContext context,
        ITripBookingService tripBookingService)
    {
        _context = context;
        _tripBookingService = tripBookingService;
    }

    // GET /api/OneDayTrip/locations
    [HttpGet("locations")]
    public async Task<IActionResult> GetLocations()
    {
        var locations = await _context.TripLocations
            .Where(l => l.IsActive)
            .OrderBy(l => l.NameAr ?? l.Name)
            .Select(l => new
            {
                id = l.Id,
                name = string.IsNullOrWhiteSpace(l.NameAr) ? l.Name : l.NameAr
            })
            .ToListAsync();

        return Ok(locations);
    }

    // GET /api/OneDayTrip/trips/{locationId}
    [HttpGet("trips/{locationId:int}")]
    public async Task<IActionResult> GetTrips(int locationId)
    {
        if (locationId <= 0)
        {
            return BadRequest(new { message = "locationId is required." });
        }

        var trips = await _context.Trips
            .Where(t => t.TripLocationId == locationId
                        && t.IsActive
                        && t.Status == TripStatus.Open)
            .OrderBy(t => t.TripDate)
            .Select(t => new
            {
                t.Id,
                t.TripDate
            })
            .ToListAsync();

        var result = trips.Select(t => new
        {
            id = t.Id,
            tripDateDisplay = t.TripDate.ToString("dddd، dd/MM/yyyy", ArabicCulture)
        });

        return Ok(result);
    }

    // GET /api/OneDayTrip/price/{tripId}?adults=X&children=Y
    [HttpGet("price/{tripId:int}")]
    public async Task<IActionResult> GetPrice(
        int tripId,
        [FromQuery] int adults = 0,
        [FromQuery] int children = 0)
    {
        if (adults < 0 || children < 0)
        {
            return BadRequest(new { message = "عدد الأشخاص غير صحيح." });
        }

        var trip = await _context.Trips
            .Where(t => t.Id == tripId)
            .Select(t => new { t.AdultTicketPrice, t.ChildTicketPrice })
            .FirstOrDefaultAsync();

        if (trip is null)
        {
            return NotFound(new { message = "الرحلة غير موجودة." });
        }

        var total = (adults * trip.AdultTicketPrice) + (children * trip.ChildTicketPrice);

        return Ok(new
        {
            adultUnitPrice = trip.AdultTicketPrice,
            childUnitPrice = trip.ChildTicketPrice,
            total
        });
    }

    // GET /api/OneDayTrip/booking-context?employeeNumber=X&tripId=Y&adults=X&children=Y
    [HttpGet("booking-context")]
    public async Task<IActionResult> GetBookingContext(
        [FromQuery] string? employeeNumber = null,
        [FromQuery] int tripId = 0,
        [FromQuery] int adults = 0,
        [FromQuery] int children = 0)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber))
        {
            return BadRequest(new { message = "employeeNumber is required." });
        }

        if (tripId <= 0)
        {
            return BadRequest(new { message = "tripId is required." });
        }

        var trip = await _context.Trips
            .Include(t => t.TripLocation)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tripId);

        if (trip is null)
        {
            return Ok(new
            {
                tripExists = false,
                isTripAvailable = false,
                canBook = false,
                message = "الرحلة غير موجودة."
            });
        }

        var isTripAvailable = trip.IsActive && trip.Status == TripStatus.Open;

        var alreadyBooked = await _context.TripBookings.AnyAsync(b =>
            b.TripId == tripId
            && b.EmployeeNumber == employeeNumber
            && b.Status == BookingStatus.Confirmed);

        var isCountValid = adults >= TripBookingRules.MinAdultsPerBooking
                           && children >= 0
                           && (adults + children) <= TripBookingRules.MaxGuestsPerBooking;

        var total = (adults * trip.AdultTicketPrice) + (children * trip.ChildTicketPrice);
        var canBook = isTripAvailable && !alreadyBooked && isCountValid;

        string? message = null;
        if (!isTripAvailable)
        {
            message = "الرحلة غير متاحة للحجز.";
        }
        else if (alreadyBooked)
        {
            message = "الموظف لديه بالفعل حجز مؤكد على هذه الرحلة.";
        }
        else if (!isCountValid)
        {
            message = $"يجب أن يحتوي الحجز على بالغ واحد على الأقل وألا يتجاوز الإجمالي {TripBookingRules.MaxGuestsPerBooking} أشخاص.";
        }

        return Ok(new
        {
            tripExists = true,
            tripId = trip.Id,
            location = new
            {
                name = trip.TripLocation is null
                    ? string.Empty
                    : (string.IsNullOrWhiteSpace(trip.TripLocation.NameAr) ? trip.TripLocation.Name : trip.TripLocation.NameAr)
            },
            tripDate = trip.TripDate,
            tripDateDisplay = trip.TripDate.ToString("dddd، dd/MM/yyyy", ArabicCulture),
            isTripAvailable,
            alreadyBooked,
            adults,
            children,
            isCountValid,
            adultUnitPrice = trip.AdultTicketPrice,
            childUnitPrice = trip.ChildTicketPrice,
            total,
            canBook,
            message
        });
    }

    // POST /api/OneDayTrip/bookings
    [HttpPost("bookings")]
    public async Task<IActionResult> CreateBooking([FromBody] TripBookingRequestDto request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "بيانات الحجز غير صحيحة." });
        }

        try
        {
            var booking = await _tripBookingService.CreateAsync(request);
            return Ok(booking);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET /api/OneDayTrip/last-trip/{employeeNumber}
    [HttpGet("last-trip/{employeeNumber}")]
    public async Task<IActionResult> GetLastTrip(string employeeNumber)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber))
        {
            return BadRequest(new { message = "employeeNumber is required." });
        }

        var lastTrip = await _context.TripBookings
            .Include(b => b.Trip)
                .ThenInclude(t => t.TripLocation)
            .Where(b => b.EmployeeNumber == employeeNumber
                        && b.Status == BookingStatus.Confirmed)
            .OrderByDescending(b => b.Trip.TripDate)
            .Select(b => new
            {
                tripDate = b.Trip.TripDate,
                locationName = b.Trip.TripLocation == null
                    ? string.Empty
                    : (string.IsNullOrWhiteSpace(b.Trip.TripLocation.NameAr)
                        ? b.Trip.TripLocation.Name
                        : b.Trip.TripLocation.NameAr)
            })
            .FirstOrDefaultAsync();

        if (lastTrip is null)
        {
            return Ok(new { hasPreviousTrip = false });
        }

        return Ok(new
        {
            hasPreviousTrip = true,
            location = new { name = lastTrip.locationName },
            tripDate = lastTrip.tripDate
        });
    }
}
