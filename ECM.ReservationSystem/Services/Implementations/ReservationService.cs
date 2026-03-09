using Microsoft.EntityFrameworkCore;
using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Models.DTOs;
using ECM.ReservationSystem.Domain.Entities;
using ECM.ReservationSystem.Services.Interfaces;

namespace ECM.ReservationSystem.Services.Implementations
{
    public class ReservationService : IReservationService
    {
        private const int MaxGuestsLimit = 6;
        private readonly ApplicationDbContext _context;
        private readonly IPricingService _pricingService;

        public ReservationService(ApplicationDbContext context, IPricingService pricingService)
        {
            _context = context;
            _pricingService = pricingService;
        }

        public async Task<List<UnitAvailabilityDto>> GetAvailableUnitsAsync(int cityId, int year, DateTime? checkInDate = null, DateTime? checkOutDate = null)
        {
            var query = _context.Units
                .Include(u => u.City)
                .Include(u => u.UnitType)
                .Include(u => u.Pricings)
                .Include(u => u.Reservations)
                .Where(u => u.IsActive && u.CityId == cityId && u.Year == year);

        

            var units = await query.ToListAsync();
            var availableUnits = new List<UnitAvailabilityDto>();

            foreach (var unit in units)
            {
                bool isAvailable = true;

                // Check availability for specific dates if provided
                if (checkInDate.HasValue && checkOutDate.HasValue)
                {
                    isAvailable = !await _context.Reservations
                        .AnyAsync(r => r.UnitId == unit.Id
                                      && r.Status != ReservationStatus.Cancelled
                                      && r.CheckInDate < checkOutDate.Value
                                      && r.CheckOutDate > checkInDate.Value);
                }

                if (isAvailable)
                {
                    var pricing = await _pricingService.GetCurrentPricingAsync(unit.Id, unit.FloorType);

                    availableUnits.Add(new UnitAvailabilityDto
                    {
                        UnitId = unit.Id,
                        UnitName = unit.Name,
                        CityName = unit.City.Name,
                        UnitTypeName = unit.UnitType.Name,
                        FloorType = unit.FloorType.ToString(),
                        DefaultCapacity = unit.DefaultCapacity,
                        MaxCapacity = unit.MaxCapacity,
                        Year = unit.Year,
                        WeeklyRentDefaultCapacity = pricing?.WeeklyRentDefaultCapacity ?? 0,
                        AdditionalPersonCost = pricing?.AdditionalPersonCost ?? 0,
                        InsuranceAmount = pricing?.InsuranceAmount ?? 0,
                        IsAvailable = isAvailable
                    });
                }
            }

            return availableUnits;
        }

        public async Task<CostCalculationDto> CalculateCostAsync(int unitId, DateTime checkInDate, DateTime checkOutDate, int numberOfGuests, bool isTransportationRequired)
        {
            if (numberOfGuests < 1 || numberOfGuests > MaxGuestsLimit)
            {
                return new CostCalculationDto { IsAvailable = false, Message = $"الحد الأقصى للأفراد هو {MaxGuestsLimit}" };
            }

            var unit = await _context.Units
                .Include(u => u.City)
                .FirstOrDefaultAsync(u => u.Id == unitId);

            if (unit == null)
            {
                return new CostCalculationDto { IsAvailable = false, Message = "الوحدة غير موجودة" };
            }

            // Check availability
            var isAvailable = !await _context.Reservations
                .AnyAsync(r => r.UnitId == unitId
                              && r.Status != ReservationStatus.Cancelled
                              && r.CheckInDate < checkOutDate
                              && r.CheckOutDate > checkInDate);

            if (!isAvailable)
            {
                return new CostCalculationDto { IsAvailable = false, Message = "الوحدة غير متاحة في هذه التواريخ" };
            }

            // Calculate costs
            var weeklyRent = await _pricingService.CalculateWeeklyRentAsync(unitId, unit.FloorType, numberOfGuests);
            var pricing = await _pricingService.GetCurrentPricingAsync(unitId, unit.FloorType);
            var transportationCost = isTransportationRequired ? await _pricingService.GetTransportationCostAsync(unit.CityId) : 0;

            return new CostCalculationDto
            {
                UnitId = unitId,
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate,
                NumberOfGuests = numberOfGuests,
                IsTransportationRequired = isTransportationRequired,
                WeeklyRent = weeklyRent,
                InsuranceAmount = pricing?.InsuranceAmount ?? 0,
                TransportationCost = transportationCost,
                TotalAmount = weeklyRent + (pricing?.InsuranceAmount ?? 0) + transportationCost,
                IsAvailable = true,
                Message = "متاح للحجز"
            };
        }

        public async Task<ReservationResponseDto> CreateReservationAsync(ReservationRequestDto request)
        {
            // Validate unit availability
            var costCalculation = await CalculateCostAsync(
                request.UnitId,
                request.CheckInDate,
                request.CheckOutDate,
                request.NumberOfGuests,
                request.IsTransportationRequired);

            if (!costCalculation.IsAvailable)
            {
                throw new InvalidOperationException(costCalculation.Message);
            }

            var unit = await _context.Units
                .Include(u => u.City)
                .FirstOrDefaultAsync(u => u.Id == request.UnitId);

            var reservation = new Reservation
            {
                EmployeeNumber = request.EmployeeNumber,
                EmployeeName = request.EmployeeName,
                Year = request.CheckInDate.Year.ToString(),
                UnitId = request.UnitId,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                NumberOfGuests = request.NumberOfGuests,
                WeeklyRent = costCalculation.WeeklyRent,
                InsuranceAmount = costCalculation.InsuranceAmount,
                TransportationCost = costCalculation.TransportationCost,
                TotalAmount = costCalculation.TotalAmount,
                IsTransportationRequired = request.IsTransportationRequired,
                Notes = request.Notes,
                Status = ReservationStatus.TemporaryHold,
                PaymentDeadline = DateTime.Now.AddHours(24), // 24 hours to pay
                CaseSystemId = request.CaseSystemId
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return new ReservationResponseDto
            {
                ReservationId = reservation.Id,
                EmployeeNumber = reservation.EmployeeNumber,
                EmployeeName = reservation.EmployeeName,
                UnitName = unit.Name,
                CityName = unit.City.Name,
                CheckInDate = reservation.CheckInDate,
                CheckOutDate = reservation.CheckOutDate,
                NumberOfGuests = reservation.NumberOfGuests,
                WeeklyRent = reservation.WeeklyRent,
                InsuranceAmount = reservation.InsuranceAmount,
                TransportationCost = reservation.TransportationCost,
                TotalAmount = reservation.TotalAmount,
                Status = reservation.Status,
                PaymentDeadline = reservation.PaymentDeadline,
                IsTransportationRequired = reservation.IsTransportationRequired,
                Notes = reservation.Notes,
                CreatedAt = reservation.CreatedAt,
                CaseSystemId = reservation.CaseSystemId
            };
        }

        public async Task<bool> ConfirmReservationAsync(int reservationId)
        {
            var reservation = await _context.Reservations.FindAsync(reservationId);
            if (reservation == null || reservation.Status != ReservationStatus.TemporaryHold)
                return false;

            reservation.Status = ReservationStatus.Confirmed;

            _context.Update(reservation);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelReservationAsync(int reservationId)
        {
            var reservation = await _context.Reservations.FindAsync(reservationId);
            if (reservation == null)
                return false;

            reservation.Status = ReservationStatus.Cancelled;

            _context.Update(reservation);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task CleanupExpiredHoldsAsync()
        {
            var expiredReservations = await _context.Reservations
                .Where(r => r.Status == ReservationStatus.TemporaryHold
                           && r.PaymentDeadline.HasValue
                           && r.PaymentDeadline < DateTime.Now)
                .ToListAsync();

            foreach (var reservation in expiredReservations)
            {
                reservation.Status = ReservationStatus.Cancelled;
            }

            if (expiredReservations.Any())
            {
                _context.UpdateRange(expiredReservations);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<ReservationResponseDto> GetReservationAsync(int reservationId)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Unit)
                .ThenInclude(u => u.City)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
                return null;

            return new ReservationResponseDto
            {
                ReservationId = reservation.Id,
                EmployeeNumber = reservation.EmployeeNumber,
                EmployeeName = reservation.EmployeeName,
                UnitName = reservation.Unit.Name,
                CityName = reservation.Unit.City.Name,
                CheckInDate = reservation.CheckInDate,
                CheckOutDate = reservation.CheckOutDate,
                NumberOfGuests = reservation.NumberOfGuests,
                WeeklyRent = reservation.WeeklyRent,
                InsuranceAmount = reservation.InsuranceAmount,
                TransportationCost = reservation.TransportationCost,
                TotalAmount = reservation.TotalAmount,
                Status = reservation.Status,
                PaymentDeadline = reservation.PaymentDeadline,
                IsTransportationRequired = reservation.IsTransportationRequired,
                Notes = reservation.Notes,
                CreatedAt = reservation.CreatedAt,
                CaseSystemId = reservation.CaseSystemId
            };
        }

        public async Task<List<ReservationResponseDto>> GetReservationsByEmployeeAsync(string employeeNumber)
        {
            var reservations = await _context.Reservations
                .Include(r => r.Unit)
                .ThenInclude(u => u.City)
                .Where(r => r.EmployeeNumber == employeeNumber)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reservations.Select(r => new ReservationResponseDto
            {
                ReservationId = r.Id,
                EmployeeNumber = r.EmployeeNumber,
                EmployeeName = r.EmployeeName,
                UnitName = r.Unit.Name,
                CityName = r.Unit.City.Name,
                CheckInDate = r.CheckInDate,
                CheckOutDate = r.CheckOutDate,
                NumberOfGuests = r.NumberOfGuests,
                WeeklyRent = r.WeeklyRent,
                InsuranceAmount = r.InsuranceAmount,
                TransportationCost = r.TransportationCost,
                TotalAmount = r.TotalAmount,
                Status = r.Status,
                PaymentDeadline = r.PaymentDeadline,
                IsTransportationRequired = r.IsTransportationRequired,
                Notes = r.Notes,
                CreatedAt = r.CreatedAt,
                CaseSystemId = r.CaseSystemId
            }).ToList();
        }
    }
}
