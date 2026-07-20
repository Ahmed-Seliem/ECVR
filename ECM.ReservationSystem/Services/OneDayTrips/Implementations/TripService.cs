using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities.OneDayTrips;
using ECM.ReservationSystem.Models.DTOs.OneDayTrips;
using ECM.ReservationSystem.Services.OneDayTrips.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECM.ReservationSystem.Services.OneDayTrips.Implementations
{
    public class TripService : ITripService
    {
        private readonly ApplicationDbContext _context;

        public TripService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<TripResponseDto>> GetAllAsync(int? locationId = null, int? year = null, int? month = null)
        {
            var query = _context.Trips
                .Include(t => t.TripLocation)
                .Include(t => t.Bookings)
                .AsNoTracking()
                .AsQueryable();

            if (locationId.HasValue)
            {
                query = query.Where(t => t.TripLocationId == locationId.Value);
            }

            if (year.HasValue)
            {
                query = query.Where(t => t.TripDate.Year == year.Value);
            }

            if (month.HasValue)
            {
                query = query.Where(t => t.TripDate.Month == month.Value);
            }

            return await query
                .OrderByDescending(t => t.TripDate)
                .Select(t => MapToDto(t))
                .ToListAsync();
        }

        public async Task<TripResponseDto?> GetByIdAsync(int id)
        {
            var trip = await _context.Trips
                .Include(t => t.TripLocation)
                .Include(t => t.Bookings)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            return trip is null ? null : MapToDto(trip);
        }

        public async Task<TripResponseDto> CreateAsync(TripRequestDto request)
        {
            var locationExists = await _context.TripLocations.AnyAsync(l => l.Id == request.TripLocationId);
            if (!locationExists)
            {
                throw new InvalidOperationException("المكان المحدد غير موجود.");
            }

            var trip = new Trip
            {
                TripLocationId = request.TripLocationId!.Value,
                TripDate = request.TripDate!.Value,
                AdultTicketPrice = request.AdultTicketPrice!.Value,
                ChildTicketPrice = request.ChildTicketPrice!.Value,
                CompanionTicketPrice = request.CompanionTicketPrice!.Value,
                Notes = request.Notes,
                IsActive = request.IsActive
            };

            _context.Trips.Add(trip);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(trip.Id) ?? MapToDto(trip);
        }

        public async Task<bool> UpdateAsync(int id, TripRequestDto request)
        {
            var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == id);
            if (trip is null)
            {
                return false;
            }

            var locationExists = await _context.TripLocations.AnyAsync(l => l.Id == request.TripLocationId);
            if (!locationExists)
            {
                throw new InvalidOperationException("المكان المحدد غير موجود.");
            }

            trip.TripLocationId = request.TripLocationId!.Value;
            trip.TripDate = request.TripDate!.Value;
            trip.AdultTicketPrice = request.AdultTicketPrice!.Value;
            trip.ChildTicketPrice = request.ChildTicketPrice!.Value;
            trip.CompanionTicketPrice = request.CompanionTicketPrice!.Value;
            trip.Notes = request.Notes;
            trip.IsActive = request.IsActive;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == id);
            if (trip is null)
            {
                return false;
            }

            var hasBookings = await _context.TripBookings.AnyAsync(b => b.TripId == id);
            if (hasBookings)
            {
                throw new InvalidOperationException("لا يمكن حذف الرحلة لأنها تحتوي على حجوزات.");
            }

            _context.Trips.Remove(trip);
            await _context.SaveChangesAsync();
            return true;
        }

        private static TripResponseDto MapToDto(Trip trip) => new()
        {
            Id = trip.Id,
            TripLocationId = trip.TripLocationId,
            LocationName = trip.TripLocation is null
                ? string.Empty
                : (string.IsNullOrWhiteSpace(trip.TripLocation.NameAr) ? trip.TripLocation.Name : trip.TripLocation.NameAr),
            TripDate = trip.TripDate,
            AdultTicketPrice = trip.AdultTicketPrice,
            ChildTicketPrice = trip.ChildTicketPrice,
            CompanionTicketPrice = trip.CompanionTicketPrice,
            Notes = trip.Notes,
            IsActive = trip.IsActive,
            BookingsCount = trip.Bookings?.Count ?? 0,
            CreatedAt = trip.CreatedAt
        };
    }
}
