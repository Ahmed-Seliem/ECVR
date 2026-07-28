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
    private readonly ITripLocationService _tripLocationService;

    public OneDayTripApiController(
        ApplicationDbContext context,
        ITripBookingService tripBookingService,
        ITripLocationService tripLocationService)
    {
        _context = context;
        _tripBookingService = tripBookingService;
        _tripLocationService = tripLocationService;
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
                        && t.IsActive)
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
        [FromQuery] int children = 0,
        [FromQuery] int companions = 0)
    {
        if (adults < 0 || children < 0 || companions < 0)
        {
            return BadRequest(new { message = "عدد الأشخاص غير صحيح." });
        }

        // Prices are defined on the place (TripLocation).
        var trip = await _context.Trips
            .Where(t => t.Id == tripId)
            .Select(t => new
            {
                t.TripLocation.AdultTicketPrice,
                t.TripLocation.ChildTicketPrice,
                t.TripLocation.CompanionTicketPrice
            })
            .FirstOrDefaultAsync();

        if (trip is null)
        {
            return NotFound(new { message = "الرحلة غير موجودة." });
        }

        var total = (adults * trip.AdultTicketPrice)
                    + (children * trip.ChildTicketPrice)
                    + (companions * trip.CompanionTicketPrice);

        return Ok(new
        {
            adultUnitPrice = trip.AdultTicketPrice,
            childUnitPrice = trip.ChildTicketPrice,
            companionUnitPrice = trip.CompanionTicketPrice,
            total
        });
    }

    // GET /api/OneDayTrip/booking-context?employeeNumber=X&tripId=Y&adults=X&children=Y
    [HttpGet("booking-context")]
    public async Task<IActionResult> GetBookingContext(
        [FromQuery] string? employeeNumber = null,
        [FromQuery] int tripId = 0,
        [FromQuery] int adults = 0,
        [FromQuery] int children = 0,
        [FromQuery] int companions = 0,
        [FromQuery] string? bookingType = null)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber))
        {
            return BadRequest(new { message = "employeeNumber is required." });
        }

        if (tripId <= 0)
        {
            return BadRequest(new { message = "tripId is required." });
        }

        var resolvedBookingType = string.Equals(bookingType, "pensions", StringComparison.OrdinalIgnoreCase)
            ? TripBookingType.Pension
            : TripBookingType.Employee;

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

        var isTripAvailable = trip.IsActive;
        var now = DateTime.Now;

        // A place can be booked once only: check any active booking on the same place (not just this trip).
        var alreadyBooked = await _context.TripBookings.AnyAsync(b =>
            b.Trip.TripLocationId == trip.TripLocationId
            && b.EmployeeNumber == employeeNumber
            && (b.Status == BookingStatus.Confirmed
                || (b.Status == BookingStatus.PendingPayment
                    && b.PaymentDeadline != null
                    && b.PaymentDeadline > now)));

        var totalGuests = adults + children + companions;
        var isCountValid = adults >= TripBookingRules.MinAdultsPerBooking
                           && children >= 0
                           && companions >= 0
                           && totalGuests <= TripBookingRules.MaxGuestsPerBooking;

        var remainingTickets = await _tripLocationService.GetRemainingTicketsAsync(trip.TripLocationId, resolvedBookingType);
        var hasEnoughTickets = totalGuests <= remainingTickets;

        var total = (adults * trip.TripLocation.AdultTicketPrice)
                    + (children * trip.TripLocation.ChildTicketPrice)
                    + (companions * trip.TripLocation.CompanionTicketPrice);
        var canBook = isTripAvailable && !alreadyBooked && isCountValid && hasEnoughTickets;

        string? message = null;
        if (!isTripAvailable)
        {
            message = "الرحلة غير متاحة للحجز.";
        }
        else if (alreadyBooked)
        {
            message = "الموظف لديه بالفعل حجز نشط على هذا المكان.";
        }
        else if (!isCountValid)
        {
            message = $"يجب أن يحتوي الحجز على بالغ واحد على الأقل وألا يتجاوز الإجمالي {TripBookingRules.MaxGuestsPerBooking} أشخاص.";
        }
        else if (!hasEnoughTickets)
        {
            message = $"لا توجد تذاكر كافية. المتبقي: {remainingTickets}.";
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
            companions,
            isCountValid,
            remainingTickets,
            hasEnoughTickets,
            adultUnitPrice = trip.TripLocation.AdultTicketPrice,
            childUnitPrice = trip.TripLocation.ChildTicketPrice,
            companionUnitPrice = trip.TripLocation.CompanionTicketPrice,
            total,
            canBook,
            message
        });
    }

    // POST /api/OneDayTrip/hold  (called by the form's confirm button BEFORE submitting the WF, so a losing
    // concurrent request is rejected here and never creates a Case document)
    [HttpPost("hold")]
    public async Task<IActionResult> Hold([FromBody] TripBookingRequestDto request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "بيانات الحجز غير صحيحة." });
        }

        try
        {
            var booking = await _tripBookingService.HoldAsync(request);
            return Ok(booking);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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

    // POST /api/OneDayTrip/update-booking  (called by the Case WF to confirm payment or cancel)
    [HttpPost("update-booking")]
    public async Task<IActionResult> UpdateBooking([FromBody] UpdateTripBookingRequest request)
    {
        if (request is null || request.DocumentId <= 0)
        {
            return BadRequest(new { message = "DocumentId مطلوب." });
        }

        if (request.BookingStatus != (int)BookingStatus.Confirmed
            && request.BookingStatus != (int)BookingStatus.Cancelled)
        {
            return BadRequest(new { message = "الحالة يجب أن تكون 2 (مؤكد) أو 3 (ملغي)." });
        }

        try
        {
            var updated = await _tripBookingService.UpdateStatusByDocumentIdAsync(
                request.DocumentId, (BookingStatus)request.BookingStatus);

            if (!updated)
            {
                return NotFound(new { message = "لا يوجد حجز مرتبط بهذا الطلب." });
            }

            return Ok(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public sealed class UpdateTripBookingRequest
    {
        public long DocumentId { get; set; }
        public int BookingStatus { get; set; }
    }

    // GET /api/OneDayTrip/booking-info/{documentId}
    // Used by the approval task form to show the payment deadline / expiry state before the admin approves.
    [HttpGet("booking-info/{documentId:long}")]
    public async Task<IActionResult> GetBookingInfo(long documentId)
    {
        if (documentId <= 0)
        {
            return BadRequest(new { message = "documentId is required." });
        }

        var booking = await _context.TripBookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.DocumentId == documentId);

        if (booking is null)
        {
            return Ok(new { exists = false });
        }

        var now = DateTime.Now;
        var isCancelled = booking.Status == BookingStatus.Cancelled;
        var isExpired = booking.Status == BookingStatus.PendingPayment
                        && booking.PaymentDeadline != null
                        && booking.PaymentDeadline <= now;
        var canConfirm = !isCancelled && !isExpired;

        return Ok(new
        {
            exists = true,
            status = (int)booking.Status,
            statusText = GetStatusText(booking.Status),
            paymentDeadline = booking.PaymentDeadline,
            paymentDeadlineDisplay = booking.PaymentDeadline?.ToString("dddd، dd/MM/yyyy hh:mm tt", ArabicCulture),
            isCancelled,
            isExpired,
            canConfirm
        });
    }

    private static string GetStatusText(BookingStatus status) => status switch
    {
        BookingStatus.PendingPayment => "بانتظار الدفع",
        BookingStatus.Confirmed => "مؤكد",
        BookingStatus.Cancelled => "ملغي",
        _ => status.ToString()
    };

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
