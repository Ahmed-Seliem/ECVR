using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities;
using ECM.ReservationSystem.Models.DTOs;
using ECM.ReservationSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ECM.ReservationSystem.Controllers.API;

[Route("api/[controller]")]
[Route("api/Reservation")]
[ApiController]
public class ReservationsApiController : ControllerBase
{
    private const int MaxGuestsLimit = 6;
    private readonly IReservationService _reservationService;
    private readonly IPricingService _pricingService;
    private readonly ApplicationDbContext _context;

    public ReservationsApiController(
        IReservationService reservationService,
        IPricingService pricingService,
        ApplicationDbContext context)
    {
        _reservationService = reservationService;
        _pricingService = pricingService;
        _context = context;
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "ok" });
    }

    [HttpGet("cities")]
    public async Task<IActionResult> GetCities()
    {
        var cities = await _context.Cities
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                id = c.Id,
                name = string.IsNullOrWhiteSpace(c.NameAr) ? c.Name : c.NameAr,
                nameEn = c.Name
            })
            .ToListAsync();

        return Ok(cities);
    }

    [HttpGet("weeks/{cityId:int}")]
    public async Task<IActionResult> GetWeeks(int cityId)
    {
        if (cityId <= 0)
        {
            return BadRequest(new { message = "cityId is required." });
        }

        var weeks = await BuildWeeksAsync(cityId);
        return Ok(weeks.Select(w => new
        {
            id = w.Id,
            weekNumber = w.DisplayName,
            weekStartDate = w.StartDate,
            weekEndDate = w.EndDate
        }));
    }

    [HttpPost("weeks")]
    public Task<IActionResult> GetWeeksPost([FromBody] WeeksRequest request)
    {
        return GetWeeks(request.CityId);
    }

    [HttpGet("floors-properties/{weekId}/flat")]
    public async Task<IActionResult> GetFloorsAndProperties(
        string weekId,
        [FromQuery] int passengers = 0)
    {
        if (passengers < 0 || passengers > MaxGuestsLimit)
        {
            return BadRequest(new { message = $"passengers must be between 0 and {MaxGuestsLimit}." });
        }

        var selectedWeek = await ResolveWeekAsync(weekId);
        if (selectedWeek is null)
        {
            return NotFound(new { message = "Invalid or unavailable week id." });
        }

        var checkInDate = selectedWeek.StartDate;
        var checkOutDate = selectedWeek.EndDate.AddDays(1);
        var year = checkInDate.Year;

        var availableUnits = await _reservationService.GetAvailableUnitsAsync(
            selectedWeek.CityId,
            year,
            checkInDate,
            checkOutDate);

        var passengerCountForPricing = Math.Max(1, passengers);
        var withTransfer = passengers > 0;
        var items = new List<object>();

        foreach (var unit in availableUnits)
        {
            var floorType = Enum.Parse<FloorType>(unit.FloorType);
            var estimatedTotal = await _pricingService.CalculateTotalCostAsync(
                unit.UnitId,
                floorType,
                passengerCountForPricing,
                withTransfer);

            items.Add(new
            {
                id = unit.UnitId,
                unitId = unit.UnitId,
                floorName = ToFloorNameArabic(floorType),
                propertyName = unit.UnitName,
                display = $"{ToFloorNameArabic(floorType)} - {unit.UnitName}",
                weeklyRentDefaultCapacity = unit.WeeklyRentDefaultCapacity,
                additionalPersonCost = unit.AdditionalPersonCost,
                insuranceAmount = unit.InsuranceAmount,
                estimatedTotal
            });
        }

        return Ok(items);
    }

    [HttpPost("floors-properties/flat")]
    public Task<IActionResult> GetFloorsAndPropertiesPost([FromBody] FloorsPropertiesRequest request)
    {
        return GetFloorsAndProperties(request.WeekId, request.Passengers);
    }

    [HttpGet("last-trip/{employeeNumber}")]
    public async Task<IActionResult> GetLastTrip(string employeeNumber)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber))
        {
            return BadRequest(new { message = "employeeNumber is required." });
        }

        var lastTrip = await _context.Reservations
            .Where(r => r.EmployeeNumber == employeeNumber)
            .OrderByDescending(r => r.CheckOutDate)
            .Select(r => new
            {
                checkOutDate = r.CheckOutDate,
                month = r.CheckOutDate.Month,
                year = r.CheckOutDate.Year
            })
            .FirstOrDefaultAsync();

        if (lastTrip is null)
        {
            return Ok(new
            {
                hasPreviousTrip = false
            });
        }

        return Ok(new
        {
            hasPreviousTrip = true,
            tripDate = lastTrip.checkOutDate,
            month = new
            {
                id = lastTrip.month,
                name = CultureInfo.GetCultureInfo("ar-EG").DateTimeFormat.GetMonthName(lastTrip.month)
            },
            year = new
            {
                id = lastTrip.year,
                name = lastTrip.year.ToString(CultureInfo.InvariantCulture)
            }
        });
    }

    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate([FromBody] ReservationCostRequest request)
    {
        if (request.NumberOfGuests < 1 || request.NumberOfGuests > MaxGuestsLimit)
        {
            return BadRequest(new { message = $"numberOfGuests must be between 1 and {MaxGuestsLimit}." });
        }

        var week = await ResolveWeekAsync(request.WeekId);
        if (week is null)
        {
            return NotFound(new { message = "Invalid or unavailable week id." });
        }

        var cost = await _reservationService.CalculateCostAsync(
            request.UnitId,
            week.StartDate,
            week.EndDate.AddDays(1),
            request.NumberOfGuests,
            request.IsTransportationRequired);

        return Ok(cost);
    }

    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] ReservationSubmissionRequest request)
    {
        if (request.NumberOfGuests < 1 || request.NumberOfGuests > MaxGuestsLimit)
        {
            return BadRequest(new { message = $"numberOfGuests must be between 1 and {MaxGuestsLimit}." });
        }

        var week = await ResolveWeekAsync(request.WeekId);
        if (week is null)
        {
            return NotFound(new { message = "Invalid or unavailable week id." });
        }

        var holdRequest = new ReservationRequestDto
        {
            EmployeeNumber = request.EmployeeNumber,
            EmployeeName = request.EmployeeName,
            UnitId = request.UnitId,
            CheckInDate = week.StartDate,
            CheckOutDate = week.EndDate.AddDays(1),
            NumberOfGuests = request.NumberOfGuests,
            IsTransportationRequired = request.IsTransportationRequired,
            Notes = request.Notes ?? string.Empty,
            CaseSystemId = request.CaseSystemId ?? string.Empty
        };

        try
        {
            var reservation = await _reservationService.CreateReservationAsync(holdRequest);
            return Ok(reservation);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("~/api/Month")]
    public IActionResult GetMonths()
    {
        var culture = CultureInfo.GetCultureInfo("ar-EG");
        var months = Enumerable.Range(1, 12)
            .Select(m => new
            {
                id = m,
                name = culture.DateTimeFormat.GetMonthName(m)
            })
            .ToList();

        return Ok(months);
    }

    [HttpGet("~/api/Year")]
    public IActionResult GetYears()
    {
        var currentYear = DateTime.UtcNow.Year;
        var years = Enumerable.Range(currentYear - 15, 16)
            .Reverse()
            .Select(y => new
            {
                id = y,
                name = y.ToString(CultureInfo.InvariantCulture)
            })
            .ToList();

        return Ok(years);
    }

    [HttpGet("slots")]
    public async Task<IActionResult> GetAvailableSlots(
        [FromQuery] int cityId,
        [FromQuery] int year,
        [FromQuery] DateTime? checkInDate,
        [FromQuery] DateTime? checkOutDate,
        [FromQuery] int numberOfGuests = 1,
        [FromQuery] bool isTransportationRequired = false)
    {
        if (cityId <= 0 || year <= 0)
        {
            return BadRequest(new { message = "cityId و year مطلوبين." });
        }

        if (numberOfGuests < 1 || numberOfGuests > MaxGuestsLimit)
        {
            return BadRequest(new { message = $"الحد الأقصى للأفراد هو {MaxGuestsLimit}." });
        }

        if (checkInDate.HasValue && checkOutDate.HasValue && checkOutDate <= checkInDate)
        {
            return BadRequest(new { message = "تاريخ المغادرة يجب أن يكون بعد تاريخ الوصول." });
        }

        var availableUnits = await _reservationService.GetAvailableUnitsAsync(cityId, year, checkInDate, checkOutDate);
        var lastTravelDate = await _context.AvailableDates
            .Where(a => a.CityId == cityId && a.IsActive)
            .OrderByDescending(a => a.AvailableTo)
            .Select(a => (DateTime?)a.AvailableTo)
            .FirstOrDefaultAsync();

        var slotItems = new List<object>();
        foreach (var unit in availableUnits)
        {
            var totalCost = await _pricingService.CalculateTotalCostAsync(
                unit.UnitId,
                Enum.Parse<ECM.ReservationSystem.Domain.Entities.FloorType>(unit.FloorType),
                numberOfGuests,
                isTransportationRequired);

            slotItems.Add(new
            {
                unitId = unit.UnitId,
                unitName = unit.UnitName,
                cityName = unit.CityName,
                unitTypeName = unit.UnitTypeName,
                floorType = unit.FloorType,
                maxGuests = MaxGuestsLimit,
                pricing = new
                {
                    weeklyRentDefaultCapacity = unit.WeeklyRentDefaultCapacity,
                    additionalPersonCost = unit.AdditionalPersonCost,
                    insuranceAmount = unit.InsuranceAmount,
                    estimatedTotal = totalCost
                }
            });
        }

        return Ok(new
        {
            cityId,
            year,
            numberOfGuests,
            maxGuestsLimit = MaxGuestsLimit,
            checkInDate,
            checkOutDate,
            lastTravelDate,
            slots = slotItems
        });
    }

    [HttpPost("hold")]
    public async Task<IActionResult> CreateHold([FromBody] ReservationRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.NumberOfGuests < 1 || request.NumberOfGuests > MaxGuestsLimit)
        {
            return BadRequest(new { message = $"الحد الأقصى للأفراد هو {MaxGuestsLimit}." });
        }

        var reservation = await _reservationService.CreateReservationAsync(request);
        return Ok(reservation);
    }

    private async Task<List<WeekOption>> BuildWeeksAsync(int cityId)
    {
        var currentDate = DateTime.UtcNow.Date;
        var availabilityWindows = await _context.AvailableDates
            .Where(a => a.CityId == cityId && a.IsActive && a.IsBookingOpen && a.BookingOpenFrom <= currentDate && a.BookingOpenTo >= currentDate)
            .OrderBy(a => a.AvailableFrom)
            .ToListAsync();

        if (!availabilityWindows.Any())
        {
            availabilityWindows = await _context.AvailableDates
                .Where(a => a.CityId == cityId && a.IsActive)
                .OrderBy(a => a.AvailableFrom)
                .ToListAsync();
        }

        var weeks = new List<WeekOption>();
        var weekNumber = 1;
        foreach (var window in availabilityWindows)
        {
            for (var start = window.AvailableFrom.Date; start <= window.AvailableTo.Date; start = start.AddDays(7))
            {
                var end = start.AddDays(6);
                if (end > window.AvailableTo.Date)
                {
                    end = window.AvailableTo.Date;
                }

                var id = $"{cityId}-{start:yyyyMMdd}";
                weeks.Add(new WeekOption
                {
                    Id = id,
                    CityId = cityId,
                    StartDate = start,
                    EndDate = end,
                    WeekNumber = weekNumber,
                    DisplayName = $"Week {weekNumber} ({start:yyyy-MM-dd} - {end:yyyy-MM-dd})"
                });
                weekNumber++;
            }
        }

        return weeks;
    }

    private async Task<WeekOption?> ResolveWeekAsync(string weekId)
    {
        if (string.IsNullOrWhiteSpace(weekId))
        {
            return null;
        }

        var parts = weekId.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !int.TryParse(parts[0], out var cityId))
        {
            return null;
        }

        var weeks = await BuildWeeksAsync(cityId);
        return weeks.FirstOrDefault(w => string.Equals(w.Id, weekId, StringComparison.OrdinalIgnoreCase));
    }

    private static string ToFloorNameArabic(FloorType floorType)
    {
        return floorType switch
        {
            FloorType.GroundFloor => "الدور الأرضي",
            FloorType.MiddleFloor => "الدور المتوسط",
            FloorType.TopFloor => "الدور العلوي",
            _ => floorType.ToString()
        };
    }

    public sealed class WeeksRequest
    {
        public int CityId { get; set; }
    }

    public sealed class FloorsPropertiesRequest
    {
        public string WeekId { get; set; } = string.Empty;
        public int Passengers { get; set; }
    }

    public sealed class ReservationCostRequest
    {
        public int UnitId { get; set; }
        public string WeekId { get; set; } = string.Empty;
        public int NumberOfGuests { get; set; } = 1;
        public bool IsTransportationRequired { get; set; }
    }

    public sealed class ReservationSubmissionRequest
    {
        public string EmployeeNumber { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public int UnitId { get; set; }
        public string WeekId { get; set; } = string.Empty;
        public int NumberOfGuests { get; set; } = 1;
        public bool IsTransportationRequired { get; set; }
        public string? Notes { get; set; }
        public string? CaseSystemId { get; set; }
    }

    private sealed class WeekOption
    {
        public string Id { get; set; } = string.Empty;
        public int CityId { get; set; }
        public int WeekNumber { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
