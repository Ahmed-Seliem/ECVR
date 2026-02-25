using Microsoft.EntityFrameworkCore;
using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Models.Entities;
using ECM.ReservationSystem.Services.Interfaces;

namespace ECM.ReservationSystem.Services.Implementations
{
    public class UnitService : IUnitService
    {
        private readonly ApplicationDbContext _context;

        public UnitService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Unit>> GetAvailableUnitsAsync(int cityId, int year)
        {
            var query = _context.Units
                .Include(u => u.City)
                .Include(u => u.UnitType)
                .Include(u => u.Pricings)
                .Where(u => u.IsActive && u.CityId == cityId && u.Year == year);

            return await query.ToListAsync();
        }

        public async Task<Unit> GetUnitByIdAsync(int unitId)
        {
            return await _context.Units
                .Include(u => u.City)
                .Include(u => u.UnitType)
                .Include(u => u.Pricings)
                .FirstOrDefaultAsync(u => u.Id == unitId && u.IsActive);
        }

        public async Task<bool> IsUnitAvailableAsync(int unitId, DateTime checkInDate, DateTime checkOutDate)
        {
            return !await _context.Reservations
                .AnyAsync(r => r.UnitId == unitId
                              && r.Status != ReservationStatus.Cancelled
                              && r.CheckInDate < checkOutDate
                              && r.CheckOutDate > checkInDate);
        }

        public async Task<List<Unit>> GetUnitsByCityAsync(int cityId)
        {
            return await _context.Units
                .Include(u => u.City)
                .Include(u => u.UnitType)
                .Where(u => u.IsActive && u.CityId == cityId)
                .OrderBy(u => u.Name)
                .ToListAsync();
        }
    }
}