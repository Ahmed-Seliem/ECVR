using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities;
using ECM.ReservationSystem.Models.DTOs;
using ECM.ReservationSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace ECM.ReservationSystem.Services.Implementations
{
    public class ReservationService : IReservationService
    {
        private const int MaxGuestsLimit = 6;
        private const int ReservationLockTimeoutMs = 15000;
        private readonly ApplicationDbContext _context;
        private readonly IPricingService _pricingService;

        public ReservationService(ApplicationDbContext context, IPricingService pricingService)
        {
            _context = context;
            _pricingService = pricingService;
        }

        public async Task<List<UnitAvailabilityDto>> GetAvailableUnitsAsync(
            int cityId,
            int year,
            DateTime? checkInDate = null,
            DateTime? checkOutDate = null,
            bool? isForManagement = null,
            bool? isForPensioners = null)
        {
            var query = _context.Units
                .Include(u => u.City)
                .Include(u => u.UnitType)
                .Include(u => u.UnitFacade)
                .Include(u => u.Pricings)
                .Include(u => u.Reservations)
                .Include(u => u.ScheduleSlots)
                .Where(u => u.IsActive && u.CityId == cityId && u.Year == year);

            if (isForManagement.HasValue)
            {
                query = query.Where(u => u.UnitType != null && u.UnitType.IsForManagement == isForManagement.Value);
            }

            if (isForPensioners.HasValue)
            {
                query = query.Where(u => u.IsForPensioners == isForPensioners.Value);
            }

            var units = await query.ToListAsync();
            var availableUnits = new List<UnitAvailabilityDto>();

            foreach (var unit in units)
            {
                bool isAvailable = true;
                List<AvailableWeekDto> availableWeeks = new();

                if (checkInDate.HasValue && checkOutDate.HasValue)
                {
                    var matchingSlot = unit.ScheduleSlots.FirstOrDefault(s =>
                        s.IsActive &&
                        s.SlotStartDate.Date == checkInDate.Value.Date &&
                        s.SlotEndDate.Date == checkOutDate.Value.Date);

                    if (matchingSlot == null)
                    {
                        isAvailable = false;
                    }

                    isAvailable = !await _context.Reservations
                        .AnyAsync(r => r.UnitId == unit.Id
                                      && r.Status != ReservationStatus.Cancelled
                                      && r.CheckInDate < checkOutDate.Value
                                      && r.CheckOutDate > checkInDate.Value)
                                  && isAvailable;

                    if (isAvailable && matchingSlot != null)
                    {
                        var pricing = await _pricingService.GetCurrentPricingAsync(unit.Id);

                        availableUnits.Add(new UnitAvailabilityDto
                        {
                            UnitId = unit.Id,
                            UnitName = unit.Name,
                            CityName = unit.City.Name,
                            UnitTypeName = unit.UnitType.Name,
                            UnitNumber = unit.Code ?? string.Empty,
                            FacadeName = unit.UnitFacade?.NameAr ?? unit.UnitFacade?.Name ?? string.Empty,
                            FloorNumber = unit.FloorNumber,
                            Capacity = unit.DefaultCapacity,
                            RoomCount = unit.RoomCount,
                            Year = unit.Year,
                            IsForPensioners = unit.IsForPensioners,
                            IsForManagement = unit.UnitType?.IsForManagement ?? false,
                            WeeklyRentDefaultCapacity = matchingSlot.WeeklyRentOverride ?? pricing?.WeeklyRentDefaultCapacity ?? 0,
                            InsuranceAmount = pricing?.InsuranceAmount ?? 0,
                            TransportationCostPerPerson = pricing?.TransportationCostPerPerson ?? 0,
                            IsAvailable = true
                        });
                    }

                    continue;
                }
                else
                {
                    var activeSlots = unit.ScheduleSlots
                        .Where(s => s.IsActive && s.Year == year)
                        .OrderBy(s => s.SlotStartDate)
                        .ToList();

                    if (activeSlots.Any())
                    {
                        foreach (var slot in activeSlots)
                        {
                            var isSlotBooked = unit.Reservations.Any(r =>
                                r.Status != ReservationStatus.Cancelled &&
                                r.CheckInDate < slot.SlotEndDate.Date &&
                                r.CheckOutDate > slot.SlotStartDate.Date);

                            if (!isSlotBooked)
                            {
                                availableWeeks.Add(new AvailableWeekDto
                                {
                                    WeekStartDate = slot.SlotStartDate,
                                    WeekEndDate = slot.SlotEndDate,
                                    IsBookingOpen = true
                                });
                            }
                        }

                        isAvailable = availableWeeks.Any();
                    }
                }

                if (isAvailable)
                {
                    var pricing = await _pricingService.GetCurrentPricingAsync(unit.Id);

                    availableUnits.Add(new UnitAvailabilityDto
                    {
                        UnitId = unit.Id,
                        UnitName = unit.Name,
                        CityName = unit.City.Name,
                        UnitTypeName = unit.UnitType.Name,
                        UnitNumber = unit.Code ?? string.Empty,
                        FacadeName = unit.UnitFacade?.NameAr ?? unit.UnitFacade?.Name ?? string.Empty,
                        FloorNumber = unit.FloorNumber,
                        Capacity = unit.DefaultCapacity,
                        RoomCount = unit.RoomCount,
                        Year = unit.Year,
                        IsForPensioners = unit.IsForPensioners,
                        IsForManagement = unit.UnitType?.IsForManagement ?? false,
                        WeeklyRentDefaultCapacity = pricing?.WeeklyRentDefaultCapacity ?? 0,
                        InsuranceAmount = pricing?.InsuranceAmount ?? 0,
                        TransportationCostPerPerson = pricing?.TransportationCostPerPerson ?? 0,
                        IsAvailable = isAvailable,
                        AvailableWeeks = availableWeeks
                    });
                }
            }

            return availableUnits;
        }

        public async Task<CostCalculationDto> CalculateCostAsync(int unitId, DateTime checkInDate, DateTime checkOutDate, int numberOfGuests, bool isTransportationRequired)
        {
            if (numberOfGuests < 0 || numberOfGuests > MaxGuestsLimit)
            {
                return new CostCalculationDto
                {
                    IsAvailable = false,
                    Message = $"عدد الأفراد يجب أن يكون بين 0 و {MaxGuestsLimit}"
                };
            }

            var guestsCount = Math.Max(0, numberOfGuests);
            var transportationRequired = isTransportationRequired && guestsCount > 0;

            var unit = await _context.Units
                .Include(u => u.City)
                .FirstOrDefaultAsync(u => u.Id == unitId);

            if (unit == null)
            {
                return new CostCalculationDto { IsAvailable = false, Message = "الوحدة غير موجودة" };
            }

            var hasScheduledSlot = await _context.UnitScheduleSlots
                .FirstOrDefaultAsync(s => s.UnitId == unitId
                                          && s.IsActive
                                          && s.SlotStartDate.Date == checkInDate.Date
                                          && s.SlotEndDate.Date == checkOutDate.Date);

            if (hasScheduledSlot == null)
            {
                return new CostCalculationDto { IsAvailable = false, Message = "الوحدة غير متاحة ضمن الجدولة المحددة." };
            }

            var isAvailable = !await _context.Reservations
                .AnyAsync(r => r.UnitId == unitId
                              && r.Status != ReservationStatus.Cancelled
                              && r.CheckInDate < checkOutDate
                              && r.CheckOutDate > checkInDate);

            if (!isAvailable)
            {
                return new CostCalculationDto { IsAvailable = false, Message = "الوحدة غير متاحة في هذه التواريخ" };
            }

            if (transportationRequired)
            {
                var quotaStatus = await GetTransportQuotaStatusAsync(unit.CityId, checkInDate, checkOutDate);
                if (!quotaStatus.HasQuotaConfigured)
                {
                    return new CostCalculationDto { IsAvailable = false, Message = "لم يتم إعداد سعة النقل لهذه المدينة في الموسم الحالي." };
                }

                if (quotaStatus.RemainingSeats < guestsCount)
                {
                    return new CostCalculationDto
                    {
                        IsAvailable = false,
                        Message = $"المقاعد المتبقية في هذا الفوج هي {quotaStatus.RemainingSeats} فقط."
                    };
                }
            }

            var weeklyRent = hasScheduledSlot.WeeklyRentOverride ?? await _pricingService.CalculateWeeklyRentAsync(unitId, guestsCount);
            var pricing = await _pricingService.GetCurrentPricingAsync(unitId);
            var transportationCost = transportationRequired
                ? await _pricingService.GetTransportationCostAsync(unit.CityId, unitId, guestsCount)
                : 0;

            return new CostCalculationDto
            {
                UnitId = unitId,
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate,
                NumberOfGuests = guestsCount,
                IsTransportationRequired = transportationRequired,
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
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            try
            {
                var unit = await _context.Units
                    .Include(u => u.City)
                    .FirstOrDefaultAsync(u => u.Id == request.UnitId);

                if (unit == null)
                {
                    throw new InvalidOperationException("الوحدة غير موجودة.");
                }

                foreach (var lockResource in BuildReservationLockResources(request, unit.CityId))
                {
                    await AcquireExclusiveReservationLockAsync(lockResource);
                }

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

                var seasonEligibility = await GetSeasonEligibilityAsync(request.EmployeeNumber, request.CheckInDate.Year);
                if (seasonEligibility.HasExistingReservation)
                {
                    throw new InvalidOperationException("تم الحجز لهذا الموظف من قبل خلال نفس الموسم.");
                }

                var reservation = new Reservation
                {
                    EmployeeNumber = request.EmployeeNumber,
                    EmployeeName = request.EmployeeName,
                    Sector = request.Sector,
                    PhoneNumber = request.PhoneNumber,
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
                    PaymentReceiptNumber = request.PaymentReceiptNumber ?? string.Empty,
                    InsuranceReceiptNumber = request.InsuranceReceiptNumber ?? string.Empty,
                    Notes = request.Notes ?? string.Empty,
                    Status = ReservationStatus.TemporaryHold,
                    PaymentDeadline = AddBusinessDays(DateTime.Now, 3),
                    CaseSystemId = request.CaseSystemId ?? string.Empty,
                    DocumentId = request.DocumentId
                };

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return MapReservationResponse(reservation, unit.Name, unit.City.Name);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> IsReservationBookedAsync(ReservationRequestDto request)
        {
            var query = _context.Reservations
                .AsNoTracking()
                .Where(r => r.Status != ReservationStatus.Cancelled);


            return await query.AnyAsync(r =>

                r.UnitId == request.UnitId &&
                r.CheckInDate == request.CheckInDate &&
                r.CheckOutDate == request.CheckOutDate);
        }

        public async Task<bool> ConfirmReservationAsync(int reservationId)
        {
            var reservation = await _context.Reservations.FindAsync(reservationId);
            if (reservation == null || reservation.Status != ReservationStatus.TemporaryHold)
            {
                return false;
            }

            reservation.Status = ReservationStatus.Confirmed;

            _context.Update(reservation);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ConfirmPaymentAsync(int reservationId)
        {
            var reservation = await _context.Reservations.FindAsync(reservationId);
            if (reservation == null)
            {
                return false;
            }

            reservation.Status = ReservationStatus.Paid;
            reservation.PaymentDeadline = null;

            var slot = await _context.UnitScheduleSlots.FirstOrDefaultAsync(s =>
                s.UnitId == reservation.UnitId &&
                s.IsActive &&
                s.SlotStartDate.Date == reservation.CheckInDate.Date &&
                s.SlotEndDate.Date == reservation.CheckOutDate.Date);

            if (slot != null)
            {
                slot.IsPaid = true;
            }

            _context.Update(reservation);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelReservationAsync(int reservationId)
        {
            var reservation = await _context.Reservations.FindAsync(reservationId);
            if (reservation == null)
            {
                return false;
            }

            reservation.Status = ReservationStatus.Cancelled;

            var slot = await _context.UnitScheduleSlots.FirstOrDefaultAsync(s =>
                s.UnitId == reservation.UnitId &&
                s.SlotStartDate.Date == reservation.CheckInDate.Date &&
                s.SlotEndDate.Date == reservation.CheckOutDate.Date);

            if (slot != null)
            {
                slot.IsPaid = false;
            }

            _context.Update(reservation);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateWorkflowStatusAsync(
            long documentId,
            ReservationStatus status,
            string? notes = null,
            string? paymentReceiptNumber = null,
            string? insuranceReceiptNumber = null)
        {
            if (documentId <= 0 || (status != ReservationStatus.Approved && status != ReservationStatus.Cancelled))
            {
                return false;
            }

            var reservation = await _context.Reservations
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync(r => r.DocumentId == documentId);

            if (reservation == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(notes))
            {
                reservation.Notes = string.IsNullOrWhiteSpace(reservation.Notes)
                    ? notes.Trim()
                    : $"{reservation.Notes} | WorkflowNotes: {notes.Trim()}";
            }

            if (!string.IsNullOrWhiteSpace(paymentReceiptNumber))
            {
                reservation.PaymentReceiptNumber = paymentReceiptNumber.Trim();
            }

            if (!string.IsNullOrWhiteSpace(insuranceReceiptNumber))
            {
                reservation.InsuranceReceiptNumber = insuranceReceiptNumber.Trim();
            }

            if (status == ReservationStatus.Cancelled)
            {
                reservation.Status = ReservationStatus.Cancelled;

                var slot = await _context.UnitScheduleSlots.FirstOrDefaultAsync(s =>
                    s.UnitId == reservation.UnitId &&
                    s.SlotStartDate.Date == reservation.CheckInDate.Date &&
                    s.SlotEndDate.Date == reservation.CheckOutDate.Date);

                if (slot != null)
                {
                    slot.IsPaid = false;
                }
            }
            else
            {
                reservation.Status = ReservationStatus.Approved;
                reservation.PaymentDeadline = null;
            }

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
                reservation.PaymentDeadline = null;
                reservation.Notes = string.IsNullOrWhiteSpace(reservation.Notes)
                    ? "Expired temporary hold cancelled automatically."
                    : $"{reservation.Notes} | Expired temporary hold cancelled automatically.";

                var slot = await _context.UnitScheduleSlots.FirstOrDefaultAsync(s =>
                    s.UnitId == reservation.UnitId &&
                    s.SlotStartDate.Date == reservation.CheckInDate.Date &&
                    s.SlotEndDate.Date == reservation.CheckOutDate.Date);

                if (slot != null)
                {
                    slot.IsPaid = false;
                }
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
            {
                return null!;
            }

            return MapReservationResponse(reservation, reservation.Unit.Name, reservation.Unit.City.Name);
        }

        public async Task<List<ReservationResponseDto>> GetReservationsByEmployeeAsync(string employeeNumber)
        {
            var reservations = await _context.Reservations
                .Include(r => r.Unit)
                .ThenInclude(u => u.City)
                .Where(r => r.EmployeeNumber == employeeNumber)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reservations
                .Select(r => MapReservationResponse(r, r.Unit.Name, r.Unit.City.Name))
                .ToList();
        }

        public async Task<TransportQuotaStatusDto> GetTransportQuotaStatusAsync(int cityId, DateTime checkInDate, DateTime checkOutDate)
        {
            var seasonYear = checkInDate.Year;
            var quota = await _context.TransportQuotas
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.CityId == cityId && q.SeasonYear == seasonYear && q.IsActive);

            var reservedSeats = await _context.Reservations
                .Include(r => r.Unit)
                .Where(r => r.Unit.CityId == cityId
                            && r.Status != ReservationStatus.Cancelled
                            && r.IsTransportationRequired
                            && r.CheckInDate < checkOutDate
                            && r.CheckOutDate > checkInDate)
                .SumAsync(r => (int?)r.NumberOfGuests) ?? 0;

            var totalSeats = quota?.TotalSeats ?? 0;

            return new TransportQuotaStatusDto
            {
                CityId = cityId,
                SeasonYear = seasonYear,
                WeekStartDate = checkInDate,
                WeekEndDate = checkOutDate,
                BusCount = quota?.BusCount ?? 0,
                SeatsPerBus = quota?.SeatsPerBus ?? 0,
                TotalSeats = totalSeats,
                ReservedSeats = reservedSeats,
                RemainingSeats = Math.Max(totalSeats - reservedSeats, 0),
                HasQuotaConfigured = quota != null
            };
        }

        public async Task<SeasonBookingEligibilityDto> GetSeasonEligibilityAsync(string employeeNumber, int seasonYear)
        {
            if (string.IsNullOrWhiteSpace(employeeNumber))
            {
                return new SeasonBookingEligibilityDto
                {
                    EmployeeNumber = string.Empty,
                    SeasonYear = seasonYear,
                    HasExistingReservation = false
                };
            }

            var reservation = await _context.Reservations
                .AsNoTracking()
                .Include(r => r.Unit)
                .ThenInclude(u => u.City)
                .Where(r => r.EmployeeNumber == employeeNumber
                            && r.Status != ReservationStatus.Cancelled
                            && r.CheckInDate.Year == seasonYear)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            if (reservation == null)
            {
                return new SeasonBookingEligibilityDto
                {
                    EmployeeNumber = employeeNumber,
                    SeasonYear = seasonYear,
                    HasExistingReservation = false
                };
            }

            return new SeasonBookingEligibilityDto
            {
                EmployeeNumber = employeeNumber,
                SeasonYear = seasonYear,
                HasExistingReservation = true,
                ReservationId = reservation.Id,
                CityId = reservation.Unit.CityId,
                CityName = reservation.Unit.City?.NameAr ?? reservation.Unit.City?.Name ?? string.Empty,
                CheckInDate = reservation.CheckInDate,
                CheckOutDate = reservation.CheckOutDate,
                Status = reservation.Status.ToString()
            };
        }

        private static DateTime AddBusinessDays(DateTime startDate, int businessDays)
        {
            var current = startDate;
            var addedDays = 0;

            while (addedDays < businessDays)
            {
                current = current.AddDays(1);

                if (current.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday)
                {
                    continue;
                }

                addedDays++;
            }

            return current;
        }

        private async Task AcquireExclusiveReservationLockAsync(string resource)
        {
            var currentTransaction = _context.Database.CurrentTransaction?.GetDbTransaction()
                ?? throw new InvalidOperationException("Reservation locking requires an active database transaction.");

            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await using var command = connection.CreateCommand();
            command.Transaction = currentTransaction;
            command.CommandText = """
                DECLARE @result int;
                EXEC @result = sp_getapplock
                    @Resource = @resource,
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Transaction',
                    @LockTimeout = @lockTimeout;
                SELECT @result;
                """;

            var resourceParameter = command.CreateParameter();
            resourceParameter.ParameterName = "@resource";
            resourceParameter.Value = resource;
            command.Parameters.Add(resourceParameter);

            var timeoutParameter = command.CreateParameter();
            timeoutParameter.ParameterName = "@lockTimeout";
            timeoutParameter.Value = ReservationLockTimeoutMs;
            command.Parameters.Add(timeoutParameter);

            var result = Convert.ToInt32(await command.ExecuteScalarAsync());
            if (result < 0)
            {
                throw new InvalidOperationException("تعذر تأمين الحجز لأن طلبًا آخر يعالج نفس الوحدة أو نفس الفترة الآن.");
            }
        }

        private static IEnumerable<string> BuildReservationLockResources(ReservationRequestDto request, int cityId)
        {
            var resources = new List<string>();

            if (!string.IsNullOrWhiteSpace(request.EmployeeNumber))
            {
                resources.Add($"reservation-lock:employee:{request.EmployeeNumber.Trim().ToUpperInvariant()}:{request.CheckInDate.Year}");
            }

            if (request.IsTransportationRequired && request.NumberOfGuests > 0)
            {
                resources.Add($"reservation-lock:transport:{cityId}:{request.CheckInDate:yyyyMMdd}:{request.CheckOutDate:yyyyMMdd}");
            }

            resources.Add($"reservation-lock:unit:{request.UnitId}:{request.CheckInDate:yyyyMMdd}:{request.CheckOutDate:yyyyMMdd}");

            return resources.OrderBy(resource => resource, StringComparer.Ordinal);
        }

        private static ReservationResponseDto MapReservationResponse(Reservation reservation, string unitName, string cityName)
        {
            return new ReservationResponseDto
            {
                ReservationId = reservation.Id,
                EmployeeNumber = reservation.EmployeeNumber,
                EmployeeName = reservation.EmployeeName,
                Sector = reservation.Sector,
                PhoneNumber = reservation.PhoneNumber,
                UnitName = unitName,
                CityName = cityName,
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
                PaymentReceiptNumber = reservation.PaymentReceiptNumber,
                InsuranceReceiptNumber = reservation.InsuranceReceiptNumber,
                Notes = reservation.Notes,
                CreatedAt = reservation.CreatedAt,
                CaseSystemId = reservation.CaseSystemId,
                DocumentId = reservation.DocumentId
            };
        }
    }
}
