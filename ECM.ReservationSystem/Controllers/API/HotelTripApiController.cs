using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities.HotelTrips;
using ECM.ReservationSystem.Domain.HotelTrips;
using ECM.ReservationSystem.Models.DTOs.HotelTrips;
using ECM.ReservationSystem.Services.HotelTrips.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ECM.ReservationSystem.Controllers.API;

[Route("api/HotelTrip")]
[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class HotelTripApiController : ControllerBase
{
    private static readonly CultureInfo ArabicCulture = CultureInfo.GetCultureInfo("ar-EG");

    private readonly ApplicationDbContext _context;
    private readonly IHotelTripBookingService _bookingService;
    private readonly IHotelService _hotelService;

    public HotelTripApiController(
        ApplicationDbContext context,
        IHotelTripBookingService bookingService,
        IHotelService hotelService)
    {
        _context = context;
        _bookingService = bookingService;
        _hotelService = hotelService;
    }

    // GET /api/HotelTrip/cities
    [HttpGet("cities")]
    public async Task<IActionResult> GetCities()
    {
        var cities = await _context.HotelCities
            .Where(c => c.IsActive)
            .OrderBy(c => c.NameAr ?? c.Name)
            .Select(c => new
            {
                id = c.Id,
                name = string.IsNullOrWhiteSpace(c.NameAr) ? c.Name : c.NameAr
            })
            .ToListAsync();

        return Ok(cities);
    }

    // GET /api/HotelTrip/hotels/{cityId}
    [HttpGet("hotels/{cityId:int}")]
    public async Task<IActionResult> GetHotels(int cityId)
    {
        if (cityId <= 0)
        {
            return BadRequest(new { message = "cityId is required." });
        }

        var hotels = await _context.Hotels
            .Where(h => h.HotelCityId == cityId && h.IsActive)
            .OrderBy(h => h.NameAr ?? h.Name)
            .Select(h => new
            {
                id = h.Id,
                name = string.IsNullOrWhiteSpace(h.NameAr) ? h.Name : h.NameAr
            })
            .ToListAsync();

        return Ok(hotels);
    }

    // GET /api/HotelTrip/trips/{hotelId}
    [HttpGet("trips/{hotelId:int}")]
    public async Task<IActionResult> GetTrips(int hotelId)
    {
        if (hotelId <= 0)
        {
            return BadRequest(new { message = "hotelId is required." });
        }

        var trips = await _context.HotelTrips
            .Where(t => t.HotelId == hotelId && t.IsActive)
            .OrderBy(t => t.StartDate)
            .Select(t => new { t.Id, t.StartDate, t.EndDate })
            .ToListAsync();

        var result = trips.Select(t => new
        {
            id = t.Id,
            tripDateDisplay = $"{t.StartDate.ToString("dd/MM/yyyy", ArabicCulture)} - {t.EndDate.ToString("dd/MM/yyyy", ArabicCulture)}"
        });

        return Ok(result);
    }

    // GET /api/HotelTrip/price/{hotelTripId}?adults=&children=&companions=
    [HttpGet("price/{hotelTripId:int}")]
    public async Task<IActionResult> GetPrice(
        int hotelTripId,
        [FromQuery] int adults = 0,
        [FromQuery] int children = 0,
        [FromQuery] int companions = 0)
    {
        if (adults < 0 || children < 0 || companions < 0)
        {
            return BadRequest(new { message = "عدد الأشخاص غير صحيح." });
        }

        var trip = await _context.HotelTrips
            .Include(t => t.Hotel)
            .Where(t => t.Id == hotelTripId)
            .Select(t => new
            {
                t.Hotel.AdultTicketPrice,
                t.Hotel.ChildTicketPrice,
                t.Hotel.CompanionTicketPrice
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

    // GET /api/HotelTrip/booking-context?employeeNumber=&hotelTripId=&adults=&children=&companions=
    [HttpGet("booking-context")]
    public async Task<IActionResult> GetBookingContext(
        [FromQuery] string? employeeNumber = null,
        [FromQuery] int hotelTripId = 0,
        [FromQuery] int adults = 0,
        [FromQuery] int children = 0,
        [FromQuery] int companions = 0,
        [FromQuery] string? bookingType = null)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber))
        {
            return BadRequest(new { message = "employeeNumber is required." });
        }

        if (hotelTripId <= 0)
        {
            return BadRequest(new { message = "hotelTripId is required." });
        }

        var resolvedBookingType = string.Equals(bookingType, "pensions", StringComparison.OrdinalIgnoreCase)
            ? HotelBookingType.Pension
            : HotelBookingType.Employee;

        var trip = await _context.HotelTrips
            .Include(t => t.Hotel)
                .ThenInclude(h => h.HotelCity)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == hotelTripId);

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

        var isTripAvailable = trip.IsActive && trip.Hotel.IsActive;
        var now = DateTime.Now;

        // A hotel can be booked once only: check any active booking on the same hotel (not just this trip).
        var alreadyBooked = await _context.HotelTripBookings.AnyAsync(b =>
            b.HotelTrip.HotelId == trip.HotelId
            && b.EmployeeNumber == employeeNumber
            && (b.Status == HotelBookingStatus.Confirmed
                || (b.Status == HotelBookingStatus.PendingPayment
                    && b.PaymentDeadline != null
                    && b.PaymentDeadline > now)));

        var totalGuests = adults + children + companions;
        var isCountValid = adults >= HotelBookingRules.MinAdultsPerBooking
                           && children >= 0
                           && companions >= 0
                           && totalGuests <= HotelBookingRules.MaxGuestsPerBooking;

        var remainingTickets = await _hotelService.GetRemainingTicketsAsync(trip.HotelId, resolvedBookingType);
        var hasEnoughTickets = totalGuests <= remainingTickets;

        var total = (adults * trip.Hotel.AdultTicketPrice)
                    + (children * trip.Hotel.ChildTicketPrice)
                    + (companions * trip.Hotel.CompanionTicketPrice);
        var canBook = isTripAvailable && !alreadyBooked && isCountValid && hasEnoughTickets;

        string? message = null;
        if (!isTripAvailable)
        {
            message = "الرحلة غير متاحة للحجز.";
        }
        else if (alreadyBooked)
        {
            message = "الموظف لديه بالفعل حجز نشط على هذا الفندق.";
        }
        else if (!isCountValid)
        {
            message = $"يجب أن يحتوي الحجز على بالغ واحد على الأقل وألا يتجاوز الإجمالي {HotelBookingRules.MaxGuestsPerBooking} أشخاص.";
        }
        else if (!hasEnoughTickets)
        {
            message = $"لا توجد تذاكر كافية. المتبقي: {remainingTickets}.";
        }

        return Ok(new
        {
            tripExists = true,
            hotelTripId = trip.Id,
            hotel = new
            {
                name = string.IsNullOrWhiteSpace(trip.Hotel.NameAr) ? trip.Hotel.Name : trip.Hotel.NameAr
            },
            startDate = trip.StartDate,
            endDate = trip.EndDate,
            tripDateDisplay = $"{trip.StartDate.ToString("dd/MM/yyyy", ArabicCulture)} - {trip.EndDate.ToString("dd/MM/yyyy", ArabicCulture)}",
            isTripAvailable,
            alreadyBooked,
            adults,
            children,
            companions,
            isCountValid,
            remainingTickets,
            hasEnoughTickets,
            adultUnitPrice = trip.Hotel.AdultTicketPrice,
            childUnitPrice = trip.Hotel.ChildTicketPrice,
            companionUnitPrice = trip.Hotel.CompanionTicketPrice,
            total,
            canBook,
            message
        });
    }

    // POST /api/HotelTrip/hold  (called by the form's confirm button BEFORE submitting the WF, so a losing
    // concurrent request is rejected here and never creates a Case document)
    [HttpPost("hold")]
    public async Task<IActionResult> Hold([FromBody] HotelTripBookingRequestDto request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "بيانات الحجز غير صحيحة." });
        }

        try
        {
            var booking = await _bookingService.HoldAsync(request);
            return Ok(booking);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // POST /api/HotelTrip/bookings
    [HttpPost("bookings")]
    public async Task<IActionResult> CreateBooking([FromBody] HotelTripBookingRequestDto request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "بيانات الحجز غير صحيحة." });
        }

        try
        {
            var booking = await _bookingService.CreateAsync(request);
            return Ok(booking);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // POST /api/HotelTrip/update-booking  (called by the Case WF to confirm payment or cancel)
    [HttpPost("update-booking")]
    public async Task<IActionResult> UpdateBooking([FromBody] UpdateHotelTripBookingRequest request)
    {
        if (request is null || request.DocumentId <= 0)
        {
            return BadRequest(new { message = "DocumentId مطلوب." });
        }

        if (request.BookingStatus != (int)HotelBookingStatus.Confirmed
            && request.BookingStatus != (int)HotelBookingStatus.Cancelled)
        {
            return BadRequest(new { message = "الحالة يجب أن تكون 2 (مؤكد) أو 3 (ملغي)." });
        }

        try
        {
            var updated = await _bookingService.UpdateStatusByDocumentIdAsync(
                request.DocumentId, (HotelBookingStatus)request.BookingStatus);

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

    public sealed class UpdateHotelTripBookingRequest
    {
        public long DocumentId { get; set; }
        public int BookingStatus { get; set; }
    }

    // GET /api/HotelTrip/booking-info/{documentId}
    // Used by the approval task form to show the payment deadline / expiry state before the admin approves.
    [HttpGet("booking-info/{documentId:long}")]
    public async Task<IActionResult> GetBookingInfo(long documentId)
    {
        if (documentId <= 0)
        {
            return BadRequest(new { message = "documentId is required." });
        }

        var booking = await _context.HotelTripBookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.DocumentId == documentId);

        if (booking is null)
        {
            return Ok(new { exists = false });
        }

        var now = DateTime.Now;
        var isCancelled = booking.Status == HotelBookingStatus.Cancelled;
        var isExpired = booking.Status == HotelBookingStatus.PendingPayment
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

    private static string GetStatusText(HotelBookingStatus status) => status switch
    {
        HotelBookingStatus.PendingPayment => "بانتظار الدفع",
        HotelBookingStatus.Confirmed => "مؤكد",
        HotelBookingStatus.Cancelled => "ملغي",
        _ => status.ToString()
    };

    // GET /api/HotelTrip/last-trip/{employeeNumber}
    [HttpGet("last-trip/{employeeNumber}")]
    public async Task<IActionResult> GetLastTrip(string employeeNumber)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber))
        {
            return BadRequest(new { message = "employeeNumber is required." });
        }

        var lastTrip = await _context.HotelTripBookings
            .Include(b => b.HotelTrip)
                .ThenInclude(t => t.Hotel)
            .Where(b => b.EmployeeNumber == employeeNumber
                        && b.Status == HotelBookingStatus.Confirmed)
            .OrderByDescending(b => b.HotelTrip.StartDate)
            .Select(b => new
            {
                startDate = b.HotelTrip.StartDate,
                endDate = b.HotelTrip.EndDate,
                hotelName = b.HotelTrip.Hotel == null
                    ? string.Empty
                    : (string.IsNullOrWhiteSpace(b.HotelTrip.Hotel.NameAr)
                        ? b.HotelTrip.Hotel.Name
                        : b.HotelTrip.Hotel.NameAr)
            })
            .FirstOrDefaultAsync();

        if (lastTrip is null)
        {
            return Ok(new { hasPreviousTrip = false });
        }

        return Ok(new
        {
            hasPreviousTrip = true,
            hotel = new { name = lastTrip.hotelName },
            startDate = lastTrip.startDate,
            endDate = lastTrip.endDate
        });
    }
}
