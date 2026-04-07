using System.Globalization;
using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities;
using ECM.ReservationSystem.Models.DTOs;
using ECM.ReservationSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECM.ReservationSystem.Controllers.API;

[Route("api/[controller]")]
[Route("api/Reservation")]
[ApiController]
public class ReservationsApiController : ControllerBase
{
    private const int MaxGuestsLimit = 6;
    private readonly IReservationService _reservationService;
    private readonly ApplicationDbContext _context;

    public ReservationsApiController(
        IReservationService reservationService,
        ApplicationDbContext context)
    {
        _reservationService = reservationService;
        _context = context;
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "ok" });
    }

    [HttpGet("cities")]
    public async Task<IActionResult> GetCities(
        [FromQuery] string? audience = null,
        [FromQuery] bool? isForManagement = null)
    {
        var filter = ResolveAudienceFilter(audience, isForManagement);

        var unitQuery = ApplyAudienceFilter(_context.Units.AsQueryable(), filter);

        var cities = await _context.Cities
            .Where(c => c.IsActive)
            .Where(c => unitQuery.Any(u =>
                u.CityId == c.Id &&
                u.IsActive &&
                u.ScheduleSlots.Any(s => s.IsActive)))
            .OrderBy(c => c.NameAr ?? c.Name)
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
    public async Task<IActionResult> GetWeeks(
        int cityId,
        [FromQuery] string? audience = null,
        [FromQuery] bool? isForManagement = null)
    {
        if (cityId <= 0)
        {
            return BadRequest(new { message = "cityId is required." });
        }

        var filter = ResolveAudienceFilter(audience, isForManagement);
        var weeks = await BuildWeeksAsync(cityId, filter);

        return Ok(weeks.Select(w => new
        {
            id = w.Id,
            weekNumber = w.DisplayName,
            weekStartDate = w.StartDate,
            weekEndDate = w.EndDate
        }));
    }

    [HttpGet("floors-properties/{weekId}/flat")]
    public async Task<IActionResult> GetAvailableUnits(
        string weekId,
        [FromQuery] string? audience = null,
        [FromQuery] bool? isForManagement = null)
    {
        var week = await ResolveWeekAsync(weekId);
        if (week is null)
        {
            return NotFound(new { message = "Invalid or unavailable week id." });
        }

        var filter = ResolveAudienceFilter(audience, isForManagement);
        var units = await _reservationService.GetAvailableUnitsAsync(
            week.CityId,
            week.StartDate.Year,
            week.StartDate,
            week.EndDate.AddDays(1),
            filter.IsForManagement,
            filter.IsForPensioners);

        var items = units
            .OrderBy(u => u.UnitName)
            .Select(u =>
            {
                var floorType = Enum.Parse<FloorType>(u.FloorType);
                return new
                {
                    id = u.UnitId,
                    unitId = u.UnitId,
                    floorName = ToFloorNameArabic(floorType),
                    propertyName = u.UnitName,
                    display = $"{ToFloorNameArabic(floorType)} - {u.UnitName}",
                    weeklyRent = u.WeeklyRentDefaultCapacity,
                    insuranceAmount = u.InsuranceAmount,
                    transportationCostPerPerson = u.TransportationCostPerPerson,
                    defaultCapacity = u.DefaultCapacity,
                    maxCapacity = u.MaxCapacity
                };
            })
            .ToList();

        return Ok(items);
    }

    [HttpGet("price/{weekId}/{unitId:int}")]
    public async Task<IActionResult> GetPrice(
        string weekId,
        int unitId,
        [FromQuery] int passengers = 1)
    {
        if (unitId <= 0)
        {
            return BadRequest(new { message = "unitId is required." });
        }

        if (passengers < 1 || passengers > MaxGuestsLimit)
        {
            return BadRequest(new { message = $"passengers must be between 1 and {MaxGuestsLimit}." });
        }

        var week = await ResolveWeekAsync(weekId);
        if (week is null)
        {
            return NotFound(new { message = "Invalid or unavailable week id." });
        }

        var cost = await _reservationService.CalculateCostAsync(
            unitId,
            week.StartDate,
            week.EndDate.AddDays(1),
            passengers,
            isTransportationRequired: true);

        if (!cost.IsAvailable)
        {
            return BadRequest(new { message = cost.Message });
        }

        return Ok(new
        {
            unitId = cost.UnitId,
            weekId,
            passengers,
            unit = cost.WeeklyRent,
            insurance = cost.InsuranceAmount,
            transportation = cost.TransportationCost,
            total = cost.TotalAmount
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

        if (!cost.IsAvailable)
        {
            return BadRequest(new { message = cost.Message });
        }

        return Ok(new
        {
            unitId = cost.UnitId,
            weekId = request.WeekId,
            passengers = cost.NumberOfGuests,
            pricing = new
            {
                unit = cost.WeeklyRent,
                insurance = cost.InsuranceAmount,
                transportation = cost.TransportationCost,
                total = cost.TotalAmount
            }
        });
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
            return Ok(new { hasPreviousTrip = false });
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
            CaseSystemId = BuildReferenceId(request),
            DocumentId = request.DocumentId
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

    [HttpPost("update-reservation")]
    public async Task<IActionResult> UpdateReservation([FromBody] UpdateReservationStatusRequest request)
    {
        if (request.DocumentId <= 0)
        {
            return BadRequest(new { message = "documentId is required." });
        }

        if (request.ReservationStatus != ReservationStatus.Approved &&
            request.ReservationStatus != ReservationStatus.Cancelled)
        {
            return BadRequest(new { message = "reservationStatus must be Approved or Cancelled." });
        }

        var success = await _reservationService.UpdateWorkflowStatusAsync(
            request.DocumentId,
            request.ReservationStatus,
            request.Notes);

        if (!success)
        {
            return NotFound(new { message = "Reservation not found for the provided documentId." });
        }

        var updatedReservation = await _context.Reservations
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .FirstAsync(r => r.DocumentId == request.DocumentId);

        return Ok(new
        {
            reservationId = updatedReservation.Id,
            referenceId = updatedReservation.CaseSystemId,
            documentId = updatedReservation.DocumentId,
            status = updatedReservation.Status.ToString()
        });
    }

    private static string BuildReferenceId(ReservationSubmissionRequest request)
    {
        if (request.WorkflowId.HasValue || request.DocumentId.HasValue)
        {
            return $"WF:{request.WorkflowId?.ToString() ?? "0"}|DOC:{request.DocumentId?.ToString() ?? "0"}";
        }

        return request.CaseSystemId ?? string.Empty;
    }

    private async Task<List<WeekOption>> BuildWeeksAsync(int cityId, AudienceFilter filter)
    {
        var slotQuery = ApplyAudienceFilter(_context.UnitScheduleSlots
            .Include(s => s.Unit)
            .ThenInclude(u => u!.UnitType), filter);

        var weeks = await slotQuery
            .Where(s => s.IsActive && s.Unit.IsActive && s.Unit.CityId == cityId)
            .Where(s => !_context.Reservations.Any(r =>
                r.UnitId == s.UnitId &&
                r.Status != ReservationStatus.Cancelled &&
                r.CheckInDate < s.SlotEndDate.AddDays(1) &&
                r.CheckOutDate > s.SlotStartDate))
            .Select(s => new { s.SlotStartDate, s.SlotEndDate })
            .Distinct()
            .OrderBy(s => s.SlotStartDate)
            .ToListAsync();

        return weeks
            .Select((week, index) => new WeekOption
            {
                Id = $"{cityId}-{week.SlotStartDate:yyyyMMdd}",
                CityId = cityId,
                WeekNumber = index + 1,
                DisplayName = $"Week {index + 1} ({week.SlotStartDate:yyyy-MM-dd} - {week.SlotEndDate:yyyy-MM-dd})",
                StartDate = week.SlotStartDate,
                EndDate = week.SlotEndDate
            })
            .ToList();
    }

    private async Task<WeekOption?> ResolveWeekAsync(string weekId)
    {
        if (string.IsNullOrWhiteSpace(weekId))
        {
            return null;
        }

        var parts = weekId.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out var cityId) ||
            !DateTime.TryParseExact(parts[1], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate))
        {
            return null;
        }

        var week = await _context.UnitScheduleSlots
            .Where(s => s.IsActive && s.Unit.CityId == cityId && s.SlotStartDate.Date == startDate.Date)
            .Select(s => new { s.SlotStartDate, s.SlotEndDate })
            .FirstOrDefaultAsync();

        if (week is null)
        {
            return null;
        }

        return new WeekOption
        {
            Id = weekId,
            CityId = cityId,
            StartDate = week.SlotStartDate,
            EndDate = week.SlotEndDate,
            DisplayName = $"Week ({week.SlotStartDate:yyyy-MM-dd} - {week.SlotEndDate:yyyy-MM-dd})"
        };
    }

    private static AudienceFilter ResolveAudienceFilter(string? audience, bool? isForManagement)
    {
        if (string.Equals(audience, "management", StringComparison.OrdinalIgnoreCase))
        {
            return new AudienceFilter(true, null);
        }

        if (string.Equals(audience, "pensioner", StringComparison.OrdinalIgnoreCase))
        {
            return new AudienceFilter(false, true);
        }

        if (string.Equals(audience, "employee", StringComparison.OrdinalIgnoreCase))
        {
            return new AudienceFilter(false, false);
        }

        return new AudienceFilter(isForManagement, null);
    }

    private static IQueryable<Unit> ApplyAudienceFilter(IQueryable<Unit> query, AudienceFilter filter)
    {
        if (filter.IsForManagement.HasValue)
        {
            query = query.Where(unit => unit.UnitType != null && unit.UnitType.IsForManagement == filter.IsForManagement.Value);
        }

        if (filter.IsForPensioners.HasValue)
        {
            query = query.Where(unit => unit.IsForPensioners == filter.IsForPensioners.Value);
        }

        return query;
    }

    private static IQueryable<UnitScheduleSlot> ApplyAudienceFilter(IQueryable<UnitScheduleSlot> query, AudienceFilter filter)
    {
        if (filter.IsForManagement.HasValue)
        {
            query = query.Where(slot => slot.Unit.UnitType != null && slot.Unit.UnitType.IsForManagement == filter.IsForManagement.Value);
        }

        if (filter.IsForPensioners.HasValue)
        {
            query = query.Where(slot => slot.Unit.IsForPensioners == filter.IsForPensioners.Value);
        }

        return query;
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

    public sealed class ReservationCostRequest
    {
        public int UnitId { get; set; }
        public string WeekId { get; set; } = string.Empty;
        public int NumberOfGuests { get; set; } = 1;
        public bool IsTransportationRequired { get; set; } = true;
    }

    public sealed class ReservationSubmissionRequest
    {
        public string EmployeeNumber { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public int UnitId { get; set; }
        public string WeekId { get; set; } = string.Empty;
        public int NumberOfGuests { get; set; } = 1;
        public bool IsTransportationRequired { get; set; } = true;
        public string? Notes { get; set; }
        public string? CaseSystemId { get; set; }
        public long? WorkflowId { get; set; }
        public long? DocumentId { get; set; }
    }

    public sealed class UpdateReservationStatusRequest
    {
        public long DocumentId { get; set; }
        public ReservationStatus ReservationStatus { get; set; }
        public string? Notes { get; set; }
    }

    private readonly record struct AudienceFilter(bool? IsForManagement, bool? IsForPensioners);

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
