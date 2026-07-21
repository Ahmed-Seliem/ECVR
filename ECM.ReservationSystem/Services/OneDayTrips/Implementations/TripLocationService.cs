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
                .Select(l => MapToDto(l, usedByLocation.TryGetValue(l.Id, out var used) ? used : (0, 0)))
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

            var used = (
                await GetUsedTicketsAsync(id, TripBookingType.Employee),
                await GetUsedTicketsAsync(id, TripBookingType.Pension));
            return MapToDto(location, used);
        }

        public async Task<int> GetRemainingTicketsAsync(int locationId, TripBookingType bookingType)
        {
            var location = await _context.TripLocations
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == locationId);

            if (location is null)
            {
                return 0;
            }

            var pool = bookingType == TripBookingType.Pension
                ? location.PensionTicketCount
                : location.EmployeeTicketCount;

            var used = await GetUsedTicketsAsync(locationId, bookingType);
            return Math.Max(0, pool - used);
        }

        public async Task<TripLocationResponseDto> CreateAsync(TripLocationRequestDto request)
        {
            var location = new TripLocation
            {
                Name = request.Name,
                NameAr = request.NameAr,
                Description = request.Description,
                AdultTicketPrice = request.AdultTicketPrice!.Value,
                ChildTicketPrice = request.ChildTicketPrice!.Value,
                CompanionTicketPrice = request.CompanionTicketPrice!.Value,
                EmployeeTicketCount = request.EmployeeTicketCount,
                PensionTicketCount = request.PensionTicketCount,
                IsActive = request.IsActive
            };

            _context.TripLocations.Add(location);
            await _context.SaveChangesAsync();

            return MapToDto(location, (0, 0));
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
            location.AdultTicketPrice = request.AdultTicketPrice!.Value;
            location.ChildTicketPrice = request.ChildTicketPrice!.Value;
            location.CompanionTicketPrice = request.CompanionTicketPrice!.Value;
            location.EmployeeTicketCount = request.EmployeeTicketCount;
            location.PensionTicketCount = request.PensionTicketCount;
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

        // Tickets consumed by active bookings (confirmed, or pending-payment not yet expired) of the given type.
        private async Task<int> GetUsedTicketsAsync(int locationId, TripBookingType bookingType)
        {
            var now = DateTime.Now;
            return await _context.TripBookings
                .Where(b => b.Trip.TripLocationId == locationId
                            && b.BookingType == bookingType
                            && (b.Status == BookingStatus.Confirmed
                                || (b.Status == BookingStatus.PendingPayment
                                    && b.PaymentDeadline != null
                                    && b.PaymentDeadline > now)))
                .SumAsync(b => b.AdultsCount + b.ChildrenCount + b.CompanionsCount);
        }

        private async Task<Dictionary<int, (int Employee, int Pension)>> GetUsedTicketsByLocationAsync()
        {
            var now = DateTime.Now;
            var grouped = await _context.TripBookings
                .Where(b => b.Status == BookingStatus.Confirmed
                            || (b.Status == BookingStatus.PendingPayment
                                && b.PaymentDeadline != null
                                && b.PaymentDeadline > now))
                .GroupBy(b => new { b.Trip.TripLocationId, b.BookingType })
                .Select(g => new
                {
                    g.Key.TripLocationId,
                    g.Key.BookingType,
                    Used = g.Sum(x => x.AdultsCount + x.ChildrenCount + x.CompanionsCount)
                })
                .ToListAsync();

            var dict = new Dictionary<int, (int Employee, int Pension)>();
            foreach (var row in grouped)
            {
                dict.TryGetValue(row.TripLocationId, out var current);
                if (row.BookingType == TripBookingType.Pension)
                {
                    current.Pension += row.Used;
                }
                else
                {
                    current.Employee += row.Used;
                }
                dict[row.TripLocationId] = current;
            }

            return dict;
        }

        private static TripLocationResponseDto MapToDto(TripLocation location, (int Employee, int Pension) used) => new()
        {
            Id = location.Id,
            Name = location.Name,
            NameAr = location.NameAr,
            Description = location.Description,
            AdultTicketPrice = location.AdultTicketPrice,
            ChildTicketPrice = location.ChildTicketPrice,
            CompanionTicketPrice = location.CompanionTicketPrice,
            EmployeeTicketCount = location.EmployeeTicketCount,
            PensionTicketCount = location.PensionTicketCount,
            EmployeeRemaining = Math.Max(0, location.EmployeeTicketCount - used.Employee),
            PensionRemaining = Math.Max(0, location.PensionTicketCount - used.Pension),
            IsActive = location.IsActive,
            TripsCount = location.Trips?.Count ?? 0,
            CreatedAt = location.CreatedAt
        };
    }
}
