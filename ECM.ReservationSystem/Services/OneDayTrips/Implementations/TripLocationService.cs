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

            var locations = await query.OrderBy(l => l.Name).ToListAsync();

            var usedByLocation = await GetUsedTicketsByLocationAsync();

            return locations
                .Select(l => MapToDto(l, usedByLocation.TryGetValue(l.Id, out var used) ? used : 0))
                .ToList();
        }

        public async Task<TripLocationResponseDto?> GetByIdAsync(int id)
        {
            var location = await _context.TripLocations
                .Include(l => l.Trips)
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == id);

            if (location is null)
            {
                return null;
            }

            var used = await GetUsedTicketsAsync(id);
            return MapToDto(location, used);
        }

        public async Task<int> GetRemainingTicketsAsync(int locationId)
        {
            var location = await _context.TripLocations
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == locationId);

            if (location is null)
            {
                return 0;
            }

            var used = await GetUsedTicketsAsync(locationId);
            return Math.Max(0, location.TicketCount - used);
        }

        public async Task<TripLocationResponseDto> CreateAsync(TripLocationRequestDto request)
        {
            var location = new TripLocation
            {
                Name = request.Name,
                NameAr = request.NameAr,
                Description = request.Description,
                TicketCount = request.TicketCount,
                IsActive = request.IsActive
            };

            _context.TripLocations.Add(location);
            await _context.SaveChangesAsync();

            return MapToDto(location, 0);
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
            location.TicketCount = request.TicketCount;
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

        // Tickets consumed by active bookings (confirmed, or pending-payment not yet expired).
        private async Task<int> GetUsedTicketsAsync(int locationId)
        {
            var now = DateTime.Now;
            return await _context.TripBookings
                .Where(b => b.Trip.TripLocationId == locationId
                            && (b.Status == BookingStatus.Confirmed
                                || (b.Status == BookingStatus.PendingPayment
                                    && b.PaymentDeadline != null
                                    && b.PaymentDeadline > now)))
                .SumAsync(b => b.AdultsCount + b.ChildrenCount + b.CompanionsCount);
        }

        private async Task<Dictionary<int, int>> GetUsedTicketsByLocationAsync()
        {
            var now = DateTime.Now;
            var grouped = await _context.TripBookings
                .Where(b => b.Status == BookingStatus.Confirmed
                            || (b.Status == BookingStatus.PendingPayment
                                && b.PaymentDeadline != null
                                && b.PaymentDeadline > now))
                .GroupBy(b => b.Trip.TripLocationId)
                .Select(g => new
                {
                    LocationId = g.Key,
                    Used = g.Sum(x => x.AdultsCount + x.ChildrenCount + x.CompanionsCount)
                })
                .ToListAsync();

            return grouped.ToDictionary(x => x.LocationId, x => x.Used);
        }

        private static TripLocationResponseDto MapToDto(TripLocation location, int usedTickets) => new()
        {
            Id = location.Id,
            Name = location.Name,
            NameAr = location.NameAr,
            Description = location.Description,
            TicketCount = location.TicketCount,
            RemainingTickets = Math.Max(0, location.TicketCount - usedTickets),
            IsActive = location.IsActive,
            TripsCount = location.Trips?.Count ?? 0,
            CreatedAt = location.CreatedAt
        };
    }
}
