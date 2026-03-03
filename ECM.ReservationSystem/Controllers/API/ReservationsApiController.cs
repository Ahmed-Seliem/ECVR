using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Models.DTOs;
using ECM.ReservationSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECM.ReservationSystem.Controllers.API;

[Route("api/[controller]")]
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
}
