using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities.HotelTrips;
using ECM.ReservationSystem.Models.DTOs.HotelTrips;
using ECM.ReservationSystem.Services.HotelTrips.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECM.ReservationSystem.Services.HotelTrips.Implementations
{
    public class HotelTripService : IHotelTripService
    {
        private readonly ApplicationDbContext _context;

        public HotelTripService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<HotelTripResponseDto>> GetAllAsync(int? hotelId = null)
        {
            var query = _context.HotelTrips
                .Include(t => t.Hotel)
                    .ThenInclude(h => h.HotelCity)
                .Include(t => t.Bookings)
                .AsNoTracking()
                .AsQueryable();

            if (hotelId.HasValue)
            {
                query = query.Where(t => t.HotelId == hotelId.Value);
            }

            return await query
                .OrderByDescending(t => t.StartDate)
                .Select(t => MapToDto(t))
                .ToListAsync();
        }

        public async Task<HotelTripResponseDto?> GetByIdAsync(int id)
        {
            var trip = await _context.HotelTrips
                .Include(t => t.Hotel)
                    .ThenInclude(h => h.HotelCity)
                .Include(t => t.Bookings)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            return trip is null ? null : MapToDto(trip);
        }

        public async Task<HotelTripResponseDto> CreateAsync(HotelTripRequestDto request)
        {
            var hotelExists = await _context.Hotels.AnyAsync(h => h.Id == request.HotelId);
            if (!hotelExists)
            {
                throw new InvalidOperationException("الفندق المحدد غير موجود.");
            }

            if (request.EndDate!.Value.Date < request.StartDate!.Value.Date)
            {
                throw new InvalidOperationException("تاريخ النهاية يجب أن يكون بعد تاريخ البداية أو مساويًا له.");
            }

            var trip = new HotelTrip
            {
                HotelId = request.HotelId!.Value,
                StartDate = request.StartDate!.Value,
                EndDate = request.EndDate!.Value,
                Notes = request.Notes,
                IsActive = request.IsActive
            };

            _context.HotelTrips.Add(trip);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(trip.Id) ?? MapToDto(trip);
        }

        public async Task<bool> UpdateAsync(int id, HotelTripRequestDto request)
        {
            var trip = await _context.HotelTrips.FirstOrDefaultAsync(t => t.Id == id);
            if (trip is null)
            {
                return false;
            }

            var hotelExists = await _context.Hotels.AnyAsync(h => h.Id == request.HotelId);
            if (!hotelExists)
            {
                throw new InvalidOperationException("الفندق المحدد غير موجود.");
            }

            if (request.EndDate!.Value.Date < request.StartDate!.Value.Date)
            {
                throw new InvalidOperationException("تاريخ النهاية يجب أن يكون بعد تاريخ البداية أو مساويًا له.");
            }

            trip.HotelId = request.HotelId!.Value;
            trip.StartDate = request.StartDate!.Value;
            trip.EndDate = request.EndDate!.Value;
            trip.Notes = request.Notes;
            trip.IsActive = request.IsActive;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var trip = await _context.HotelTrips.FirstOrDefaultAsync(t => t.Id == id);
            if (trip is null)
            {
                return false;
            }

            var hasBookings = await _context.HotelTripBookings.AnyAsync(b => b.HotelTripId == id);
            if (hasBookings)
            {
                throw new InvalidOperationException("لا يمكن حذف الرحلة لأنها تحتوي على حجوزات.");
            }

            _context.HotelTrips.Remove(trip);
            await _context.SaveChangesAsync();
            return true;
        }

        private static HotelTripResponseDto MapToDto(HotelTrip trip) => new()
        {
            Id = trip.Id,
            HotelId = trip.HotelId,
            HotelName = trip.Hotel is null
                ? string.Empty
                : (string.IsNullOrWhiteSpace(trip.Hotel.NameAr) ? trip.Hotel.Name : trip.Hotel.NameAr),
            CityName = trip.Hotel?.HotelCity is null
                ? string.Empty
                : (string.IsNullOrWhiteSpace(trip.Hotel.HotelCity.NameAr) ? trip.Hotel.HotelCity.Name : trip.Hotel.HotelCity.NameAr),
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            Notes = trip.Notes,
            IsActive = trip.IsActive,
            BookingsCount = trip.Bookings?.Count ?? 0,
            CreatedAt = trip.CreatedAt
        };
    }
}
