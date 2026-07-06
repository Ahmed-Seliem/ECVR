using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities.OneDayTrips;
using ECM.ReservationSystem.Models.DTOs.OneDayTrips;
using ECM.ReservationSystem.Services.OneDayTrips.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECM.ReservationSystem.Services.OneDayTrips.Implementations
{
    public class TripLocationService : ITripLocationService
    {
        private readonly ApplicationDbContext _context;

        public TripLocationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<TripLocationResponseDto>> GetAllAsync(bool includeInactive = true)
        {
            var query = _context.TripLocations
                .Include(l => l.Trips)
                .AsNoTracking()
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(l => l.IsActive);
            }

            return await query
                .OrderBy(l => l.Name)
                .Select(l => MapToDto(l))
                .ToListAsync();
        }

        public async Task<TripLocationResponseDto?> GetByIdAsync(int id)
        {
            var location = await _context.TripLocations
                .Include(l => l.Trips)
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == id);

            return location is null ? null : MapToDto(location);
        }

        public async Task<TripLocationResponseDto> CreateAsync(TripLocationRequestDto request)
        {
            var location = new TripLocation
            {
                Name = request.Name,
                NameAr = request.NameAr,
                Description = request.Description,
                IsActive = request.IsActive
            };

            _context.TripLocations.Add(location);
            await _context.SaveChangesAsync();

            return MapToDto(location);
        }

        public async Task<bool> UpdateAsync(int id, TripLocationRequestDto request)
        {
            var location = await _context.TripLocations.FirstOrDefaultAsync(l => l.Id == id);
            if (location is null)
            {
                return false;
            }

            location.Name = request.Name;
            location.NameAr = request.NameAr;
            location.Description = request.Description;
            location.IsActive = request.IsActive;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var location = await _context.TripLocations.FirstOrDefaultAsync(l => l.Id == id);
            if (location is null)
            {
                return false;
            }

            var hasTrips = await _context.Trips.AnyAsync(t => t.TripLocationId == id);
            if (hasTrips)
            {
                throw new InvalidOperationException("لا يمكن حذف المكان لأنه مرتبط برحلات.");
            }

            _context.TripLocations.Remove(location);
            await _context.SaveChangesAsync();
            return true;
        }

        private static TripLocationResponseDto MapToDto(TripLocation location) => new()
        {
            Id = location.Id,
            Name = location.Name,
            NameAr = location.NameAr,
            Description = location.Description,
            IsActive = location.IsActive,
            TripsCount = location.Trips?.Count ?? 0,
            CreatedAt = location.CreatedAt
        };
    }
}
