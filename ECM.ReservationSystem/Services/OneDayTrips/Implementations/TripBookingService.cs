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

        public TripBookingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<TripBookingResponseDto>> GetAllAsync(int? tripId = null)
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
            // ===== Business validation (service layer, beside the DB check constraints) =====
            if (request.AdultsCount < TripBookingRules.MinAdultsPerBooking)
            {
                throw new InvalidOperationException("يجب أن يحتوي الحجز على بالغ واحد على الأقل.");
            }

            if (request.ChildrenCount < 0)
            {
                throw new InvalidOperationException("عدد الأطفال لا يمكن أن يكون بالسالب.");
            }

            if (request.AdultsCount + request.ChildrenCount > TripBookingRules.MaxGuestsPerBooking)
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

            if (!trip.IsActive || trip.Status != TripStatus.Open)
            {
                throw new InvalidOperationException("الرحلة غير متاحة للحجز.");
            }

            var alreadyBooked = await _context.TripBookings.AnyAsync(b =>
                b.TripId == trip.Id
                && b.EmployeeNumber == request.EmployeeNumber
                && b.Status == BookingStatus.Confirmed);

            if (alreadyBooked)
            {
                throw new InvalidOperationException("الموظف لديه بالفعل حجز مؤكد على هذه الرحلة.");
            }

            var totalAmount = (request.AdultsCount * trip.AdultTicketPrice)
                            + (request.ChildrenCount * trip.ChildTicketPrice);

            var booking = new TripBooking
            {
                TripId = trip.Id,
                EmployeeNumber = request.EmployeeNumber,
                EmployeeName = request.EmployeeName,
                Sector = request.Sector,
                PhoneNumber = request.PhoneNumber,
                AdultsCount = request.AdultsCount,
                ChildrenCount = request.ChildrenCount,
                AdultUnitPrice = trip.AdultTicketPrice,
                ChildUnitPrice = trip.ChildTicketPrice,
                TotalAmount = totalAmount,
                Status = BookingStatus.Confirmed,
                CaseSystemId = request.CaseSystemId,
                WorkflowId = request.WorkflowId,
                DocumentId = request.DocumentId,
                Notes = request.Notes
            };

            _context.TripBookings.Add(booking);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(booking.Id) ?? MapToDto(booking);
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
            AdultUnitPrice = booking.AdultUnitPrice,
            ChildUnitPrice = booking.ChildUnitPrice,
            TotalAmount = booking.TotalAmount,
            Status = booking.Status,
            CaseSystemId = booking.CaseSystemId,
            WorkflowId = booking.WorkflowId,
            DocumentId = booking.DocumentId,
            Notes = booking.Notes,
            CreatedAt = booking.CreatedAt
        };
    }
}
