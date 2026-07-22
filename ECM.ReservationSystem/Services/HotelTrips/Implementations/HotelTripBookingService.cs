using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities.HotelTrips;
using ECM.ReservationSystem.Domain.HotelTrips;
using ECM.ReservationSystem.Models.DTOs.HotelTrips;
using ECM.ReservationSystem.Services.HotelTrips.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECM.ReservationSystem.Services.HotelTrips.Implementations
{
    public class HotelTripBookingService : IHotelTripBookingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHotelService _hotelService;

        public HotelTripBookingService(ApplicationDbContext context, IHotelService hotelService)
        {
            _context = context;
            _hotelService = hotelService;
        }

        public async Task<List<HotelTripBookingResponseDto>> GetAllAsync(int? hotelTripId = null, HotelBookingType? bookingType = null)
        {
            var query = _context.HotelTripBookings
                .Include(b => b.HotelTrip)
                    .ThenInclude(t => t.Hotel)
                        .ThenInclude(h => h.HotelCity)
                .AsNoTracking()
                .AsQueryable();

            if (hotelTripId.HasValue)
            {
                query = query.Where(b => b.HotelTripId == hotelTripId.Value);
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

        public async Task<HotelTripBookingResponseDto?> GetByIdAsync(int id)
        {
            var booking = await _context.HotelTripBookings
                .Include(b => b.HotelTrip)
                    .ThenInclude(t => t.Hotel)
                        .ThenInclude(h => h.HotelCity)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            return booking is null ? null : MapToDto(booking);
        }

        public async Task<List<HotelTripBookingResponseDto>> GetByEmployeeAsync(string employeeNumber)
        {
            return await _context.HotelTripBookings
                .Include(b => b.HotelTrip)
                    .ThenInclude(t => t.Hotel)
                        .ThenInclude(h => h.HotelCity)
                .AsNoTracking()
                .Where(b => b.EmployeeNumber == employeeNumber)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => MapToDto(b))
                .ToListAsync();
        }

        public async Task<HotelTripBookingResponseDto> CreateAsync(HotelTripBookingRequestDto request)
        {
            var totalGuests = request.AdultsCount + request.ChildrenCount + request.CompanionsCount;

            if (request.AdultsCount < HotelBookingRules.MinAdultsPerBooking)
            {
                throw new InvalidOperationException("يجب أن يحتوي الحجز على بالغ واحد على الأقل.");
            }

            if (request.ChildrenCount < 0 || request.CompanionsCount < 0)
            {
                throw new InvalidOperationException("عدد الأطفال أو المرافقين لا يمكن أن يكون بالسالب.");
            }

            if (totalGuests > HotelBookingRules.MaxGuestsPerBooking)
            {
                throw new InvalidOperationException(
                    $"إجمالي عدد الأشخاص لا يمكن أن يتجاوز {HotelBookingRules.MaxGuestsPerBooking}.");
            }

            var trip = await _context.HotelTrips
                .Include(t => t.Hotel)
                .FirstOrDefaultAsync(t => t.Id == request.HotelTripId);

            if (trip is null)
            {
                throw new InvalidOperationException("الرحلة غير موجودة.");
            }

            if (!trip.IsActive || !trip.Hotel.IsActive)
            {
                throw new InvalidOperationException("الرحلة غير متاحة للحجز.");
            }

            var now = DateTime.Now;
            // A hotel can be booked once only: block if the employee already has an active booking on
            // ANY trip of the same hotel (not just this specific trip).
            var alreadyBooked = await _context.HotelTripBookings.AnyAsync(b =>
                b.HotelTrip.HotelId == trip.HotelId
                && b.EmployeeNumber == request.EmployeeNumber
                && (b.Status == HotelBookingStatus.Confirmed
                    || (b.Status == HotelBookingStatus.PendingPayment
                        && b.PaymentDeadline != null
                        && b.PaymentDeadline > now)));

            if (alreadyBooked)
            {
                throw new InvalidOperationException("الموظف لديه بالفعل حجز نشط على هذا الفندق.");
            }

            var bookingType = string.Equals(request.BookingType, "pensions", StringComparison.OrdinalIgnoreCase)
                ? HotelBookingType.Pension
                : HotelBookingType.Employee;

            // Ticket availability: each booking type has its own pool on the hotel (1 ticket per person).
            var remaining = await _hotelService.GetRemainingTicketsAsync(trip.HotelId, bookingType);
            if (totalGuests > remaining)
            {
                throw new InvalidOperationException($"لا توجد تذاكر كافية. المتبقي: {remaining}.");
            }

            var baseTotal = (request.AdultsCount * trip.Hotel.AdultTicketPrice)
                          + (request.ChildrenCount * trip.Hotel.ChildTicketPrice)
                          + (request.CompanionsCount * trip.Hotel.CompanionTicketPrice);

            // Pension surcharge (10%) is postponed for now — total = base regardless of type.
            // var totalAmount = bookingType == HotelBookingType.Pension
            //     ? Math.Round(baseTotal * (1 + (HotelBookingRules.PensionSurchargePercent / 100m)), 2)
            //     : baseTotal;
            var totalAmount = baseTotal;

            var booking = new HotelTripBooking
            {
                HotelTripId = trip.Id,
                EmployeeNumber = request.EmployeeNumber,
                EmployeeName = request.EmployeeName,
                Sector = request.Sector,
                PhoneNumber = request.PhoneNumber,
                AdultsCount = request.AdultsCount,
                ChildrenCount = request.ChildrenCount,
                CompanionsCount = request.CompanionsCount,
                AdultUnitPrice = trip.Hotel.AdultTicketPrice,
                ChildUnitPrice = trip.Hotel.ChildTicketPrice,
                CompanionUnitPrice = trip.Hotel.CompanionTicketPrice,
                TotalAmount = totalAmount,
                BookingType = bookingType,
                Status = HotelBookingStatus.PendingPayment,
                PaymentDeadline = HotelBookingRules.ComputePaymentDeadline(now),
                CaseSystemId = request.CaseSystemId,
                WorkflowId = request.WorkflowId,
                DocumentId = request.DocumentId,
                Notes = request.Notes
            };

            _context.HotelTripBookings.Add(booking);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(booking.Id) ?? MapToDto(booking);
        }

        public async Task<bool> ConfirmPaymentAsync(int id)
        {
            var booking = await _context.HotelTripBookings.FirstOrDefaultAsync(b => b.Id == id);
            if (booking is null)
            {
                return false;
            }

            if (booking.Status == HotelBookingStatus.Cancelled)
            {
                throw new InvalidOperationException("لا يمكن تأكيد دفع حجز ملغي.");
            }

            booking.Status = HotelBookingStatus.Confirmed;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelAsync(int id)
        {
            var booking = await _context.HotelTripBookings.FirstOrDefaultAsync(b => b.Id == id);
            if (booking is null)
            {
                return false;
            }

            booking.Status = HotelBookingStatus.Cancelled;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetStatusAsync(int id, HotelBookingStatus status)
        {
            var booking = await _context.HotelTripBookings.FirstOrDefaultAsync(b => b.Id == id);
            if (booking is null)
            {
                return false;
            }

            booking.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStatusByDocumentIdAsync(long documentId, HotelBookingStatus status)
        {
            if (status != HotelBookingStatus.Confirmed && status != HotelBookingStatus.Cancelled)
            {
                throw new InvalidOperationException("الحالة المطلوبة غير صحيحة.");
            }

            var booking = await _context.HotelTripBookings
                .FirstOrDefaultAsync(b => b.DocumentId == documentId);

            if (booking is null)
            {
                return false;
            }

            // Rejection is always allowed.
            if (status == HotelBookingStatus.Cancelled)
            {
                booking.Status = HotelBookingStatus.Cancelled;
                await _context.SaveChangesAsync();
                return true;
            }

            // Confirming: guard against approving a booking whose payment window has already closed
            // (otherwise a stale WF approval could resurrect a cancelled booking and overbook released tickets).
            if (booking.Status == HotelBookingStatus.Cancelled)
            {
                throw new InvalidOperationException("تم إلغاء الحجز لانتهاء مهلة الدفع، ولا يمكن تأكيده.");
            }

            if (booking.Status == HotelBookingStatus.PendingPayment
                && booking.PaymentDeadline != null
                && booking.PaymentDeadline <= DateTime.Now)
            {
                // Expired but not yet auto-cancelled: cancel it now so its tickets stay released, then reject the confirm.
                booking.Status = HotelBookingStatus.Cancelled;
                await _context.SaveChangesAsync();
                throw new InvalidOperationException("انتهت مهلة الدفع، تم إلغاء الحجز ولا يمكن تأكيده.");
            }

            booking.Status = HotelBookingStatus.Confirmed;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CancelExpiredAsync()
        {
            var now = DateTime.Now;
            var expired = await _context.HotelTripBookings
                .Where(b => b.Status == HotelBookingStatus.PendingPayment
                            && b.PaymentDeadline != null
                            && b.PaymentDeadline <= now)
                .ToListAsync();

            if (expired.Count == 0)
            {
                return 0;
            }

            foreach (var booking in expired)
            {
                booking.Status = HotelBookingStatus.Cancelled;
            }

            await _context.SaveChangesAsync();
            return expired.Count;
        }

        private static HotelTripBookingResponseDto MapToDto(HotelTripBooking booking) => new()
        {
            Id = booking.Id,
            HotelTripId = booking.HotelTripId,
            HotelName = booking.HotelTrip?.Hotel is null
                ? string.Empty
                : (string.IsNullOrWhiteSpace(booking.HotelTrip.Hotel.NameAr)
                    ? booking.HotelTrip.Hotel.Name
                    : booking.HotelTrip.Hotel.NameAr),
            CityName = booking.HotelTrip?.Hotel?.HotelCity is null
                ? string.Empty
                : (string.IsNullOrWhiteSpace(booking.HotelTrip.Hotel.HotelCity.NameAr)
                    ? booking.HotelTrip.Hotel.HotelCity.Name
                    : booking.HotelTrip.Hotel.HotelCity.NameAr),
            StartDate = booking.HotelTrip?.StartDate ?? default,
            EndDate = booking.HotelTrip?.EndDate ?? default,
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
