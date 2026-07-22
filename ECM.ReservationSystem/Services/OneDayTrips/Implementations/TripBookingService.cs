using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities.OneDayTrips;
using ECM.ReservationSystem.Domain.OneDayTrips;
using ECM.ReservationSystem.Models.DTOs.OneDayTrips;
using ECM.ReservationSystem.Services.OneDayTrips.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECM.ReservationSystem.Services.OneDayTrips.Implementations
{
    public class TripBookingService : ITripBookingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITripLocationService _tripLocationService;

        public TripBookingService(ApplicationDbContext context, ITripLocationService tripLocationService)
        {
            _context = context;
            _tripLocationService = tripLocationService;
        }

        public async Task<List<TripBookingResponseDto>> GetAllAsync(int? tripId = null, TripBookingType? bookingType = null)
        {
            var query = _context.TripBookings
                .Include(b => b.Trip)
                    .ThenInclude(t => t.TripLocation)
                .AsNoTracking()
                .AsQueryable();

            if (tripId.HasValue)
            {
                query = query.Where(b => b.TripId == tripId.Value);
            }

            if (bookingType.HasValue)
            {
                query = query.Where(b => b.BookingType == bookingType.Value);
            }

            return await query
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => MapToDto(b))
                .ToListAsync();
        }

        public async Task<TripBookingResponseDto?> GetByIdAsync(int id)
        {
            var booking = await _context.TripBookings
                .Include(b => b.Trip)
                    .ThenInclude(t => t.TripLocation)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            return booking is null ? null : MapToDto(booking);
        }

        public async Task<List<TripBookingResponseDto>> GetByEmployeeAsync(string employeeNumber)
        {
            return await _context.TripBookings
                .Include(b => b.Trip)
                    .ThenInclude(t => t.TripLocation)
                .AsNoTracking()
                .Where(b => b.EmployeeNumber == employeeNumber)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => MapToDto(b))
                .ToListAsync();
        }

        public async Task<TripBookingResponseDto> CreateAsync(TripBookingRequestDto request)
        {
            var totalGuests = request.AdultsCount + request.ChildrenCount + request.CompanionsCount;

            // ===== Business validation (service layer, beside the DB check constraints) =====
            if (request.AdultsCount < TripBookingRules.MinAdultsPerBooking)
            {
                throw new InvalidOperationException("يجب أن يحتوي الحجز على بالغ واحد على الأقل.");
            }

            if (request.ChildrenCount < 0 || request.CompanionsCount < 0)
            {
                throw new InvalidOperationException("عدد الأطفال أو المرافقين لا يمكن أن يكون بالسالب.");
            }

            if (totalGuests > TripBookingRules.MaxGuestsPerBooking)
            {
                throw new InvalidOperationException(
                    $"إجمالي عدد الأشخاص لا يمكن أن يتجاوز {TripBookingRules.MaxGuestsPerBooking}.");
            }

            var trip = await _context.Trips
                .Include(t => t.TripLocation)
                .FirstOrDefaultAsync(t => t.Id == request.TripId);

            if (trip is null)
            {
                throw new InvalidOperationException("الرحلة غير موجودة.");
            }

            if (!trip.IsActive)
            {
                throw new InvalidOperationException("الرحلة غير متاحة للحجز.");
            }

            var now = DateTime.Now;
            // A place (location) can be booked once only: block if the employee already has an active
            // booking on ANY trip of the same place (not just the same trip date).
            var alreadyBooked = await _context.TripBookings.AnyAsync(b =>
                b.Trip.TripLocationId == trip.TripLocationId
                && b.EmployeeNumber == request.EmployeeNumber
                && (b.Status == BookingStatus.Confirmed
                    || (b.Status == BookingStatus.PendingPayment
                        && b.PaymentDeadline != null
                        && b.PaymentDeadline > now)));

            if (alreadyBooked)
            {
                throw new InvalidOperationException("الموظف لديه بالفعل حجز نشط على هذا المكان.");
            }

            var bookingType = string.Equals(request.BookingType, "pensions", StringComparison.OrdinalIgnoreCase)
                ? TripBookingType.Pension
                : TripBookingType.Employee;

            // Ticket availability: each booking type has its own pool on the location (1 ticket per person).
            var remaining = await _tripLocationService.GetRemainingTicketsAsync(trip.TripLocationId, bookingType);
            if (totalGuests > remaining)
            {
                throw new InvalidOperationException($"لا توجد تذاكر كافية. المتبقي: {remaining}.");
            }

            // Prices are defined on the place (TripLocation).
            var location = trip.TripLocation;
            var baseTotal = (request.AdultsCount * location.AdultTicketPrice)
                          + (request.ChildrenCount * location.ChildTicketPrice)
                          + (request.CompanionsCount * location.CompanionTicketPrice);

            // Pension surcharge (10%) is postponed for now — total = base regardless of type.
            // var totalAmount = bookingType == TripBookingType.Pension
            //     ? Math.Round(baseTotal * (1 + (TripBookingRules.PensionSurchargePercent / 100m)), 2)
            //     : baseTotal;
            var totalAmount = baseTotal;

            var booking = new TripBooking
            {
                TripId = trip.Id,
                EmployeeNumber = request.EmployeeNumber,
                EmployeeName = request.EmployeeName,
                Sector = request.Sector,
                PhoneNumber = request.PhoneNumber,
                AdultsCount = request.AdultsCount,
                ChildrenCount = request.ChildrenCount,
                CompanionsCount = request.CompanionsCount,
                AdultUnitPrice = location.AdultTicketPrice,
                ChildUnitPrice = location.ChildTicketPrice,
                CompanionUnitPrice = location.CompanionTicketPrice,
                TotalAmount = totalAmount,
                BookingType = bookingType,
                Status = BookingStatus.PendingPayment,
                PaymentDeadline = TripBookingRules.ComputePaymentDeadline(now),
                CaseSystemId = request.CaseSystemId,
                WorkflowId = request.WorkflowId,
                DocumentId = request.DocumentId,
                Notes = request.Notes
            };

            _context.TripBookings.Add(booking);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(booking.Id) ?? MapToDto(booking);
        }

        public async Task<bool> ConfirmPaymentAsync(int id)
        {
            var booking = await _context.TripBookings.FirstOrDefaultAsync(b => b.Id == id);
            if (booking is null)
            {
                return false;
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                throw new InvalidOperationException("لا يمكن تأكيد دفع حجز ملغي.");
            }

            booking.Status = BookingStatus.Confirmed;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelAsync(int id)
        {
            var booking = await _context.TripBookings.FirstOrDefaultAsync(b => b.Id == id);
            if (booking is null)
            {
                return false;
            }

            booking.Status = BookingStatus.Cancelled;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetStatusAsync(int id, BookingStatus status)
        {
            var booking = await _context.TripBookings.FirstOrDefaultAsync(b => b.Id == id);
            if (booking is null)
            {
                return false;
            }

            booking.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStatusByDocumentIdAsync(long documentId, BookingStatus status)
        {
            if (status != BookingStatus.Confirmed && status != BookingStatus.Cancelled)
            {
                throw new InvalidOperationException("الحالة المطلوبة غير صحيحة.");
            }

            var booking = await _context.TripBookings
                .FirstOrDefaultAsync(b => b.DocumentId == documentId);

            if (booking is null)
            {
                return false;
            }

            // Rejection is always allowed.
            if (status == BookingStatus.Cancelled)
            {
                booking.Status = BookingStatus.Cancelled;
                await _context.SaveChangesAsync();
                return true;
            }

            // Confirming: guard against approving a booking whose payment window has already closed
            // (otherwise a stale WF approval could resurrect a cancelled booking and overbook released tickets).
            if (booking.Status == BookingStatus.Cancelled)
            {
                throw new InvalidOperationException("تم إلغاء الحجز لانتهاء مهلة الدفع، ولا يمكن تأكيده.");
            }

            if (booking.Status == BookingStatus.PendingPayment
                && booking.PaymentDeadline != null
                && booking.PaymentDeadline <= DateTime.Now)
            {
                // Expired but not yet auto-cancelled: cancel it now so its tickets stay released, then reject the confirm.
                booking.Status = BookingStatus.Cancelled;
                await _context.SaveChangesAsync();
                throw new InvalidOperationException("انتهت مهلة الدفع، تم إلغاء الحجز ولا يمكن تأكيده.");
            }

            booking.Status = BookingStatus.Confirmed;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CancelExpiredAsync()
        {
            var now = DateTime.Now;
            var expired = await _context.TripBookings
                .Where(b => b.Status == BookingStatus.PendingPayment
                            && b.PaymentDeadline != null
                            && b.PaymentDeadline <= now)
                .ToListAsync();

            if (expired.Count == 0)
            {
                return 0;
            }

            foreach (var booking in expired)
            {
                booking.Status = BookingStatus.Cancelled;
            }

            await _context.SaveChangesAsync();
            return expired.Count;
        }

        private static TripBookingResponseDto MapToDto(TripBooking booking) => new()
        {
            Id = booking.Id,
            TripId = booking.TripId,
            LocationName = booking.Trip?.TripLocation is null
                ? string.Empty
                : (string.IsNullOrWhiteSpace(booking.Trip.TripLocation.NameAr)
                    ? booking.Trip.TripLocation.Name
                    : booking.Trip.TripLocation.NameAr),
            TripDate = booking.Trip?.TripDate ?? default,
            EmployeeNumber = booking.EmployeeNumber,
            EmployeeName = booking.EmployeeName,
            Sector = booking.Sector,
            PhoneNumber = booking.PhoneNumber,
            AdultsCount = booking.AdultsCount,
            ChildrenCount = booking.ChildrenCount,
            CompanionsCount = booking.CompanionsCount,
            AdultUnitPrice = booking.AdultUnitPrice,
            ChildUnitPrice = booking.ChildUnitPrice,
            CompanionUnitPrice = booking.CompanionUnitPrice,
            TotalAmount = booking.TotalAmount,
            BookingType = booking.BookingType,
            Status = booking.Status,
            PaymentDeadline = booking.PaymentDeadline,
            CaseSystemId = booking.CaseSystemId,
            WorkflowId = booking.WorkflowId,
            DocumentId = booking.DocumentId,
            Notes = booking.Notes,
            CreatedAt = booking.CreatedAt
        };
    }
}
