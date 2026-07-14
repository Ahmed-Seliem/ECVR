using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities.HotelTrips;
using ECM.ReservationSystem.Models.DTOs.HotelTrips;
using ECM.ReservationSystem.Services.HotelTrips.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECM.ReservationSystem.Services.HotelTrips.Implementations
{
    public class HotelCityService : IHotelCityService
    {
        private readonly ApplicationDbContext _context;

        public HotelCityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<HotelCityResponseDto>> GetAllAsync(bool includeInactive = true)
        {
            var query = _context.HotelCities
                .Include(c => c.Hotels)
                .AsNoTracking()
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            return await query
                .OrderBy(c => c.Name)
                .Select(c => MapToDto(c))
                .ToListAsync();
        }

        public async Task<HotelCityResponseDto?> GetByIdAsync(int id)
        {
            var city = await _context.HotelCities
                .Include(c => c.Hotels)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            return city is null ? null : MapToDto(city);
        }

        public async Task<HotelCityResponseDto> CreateAsync(HotelCityRequestDto request)
        {
            var city = new HotelCity
            {
                Name = request.Name,
                NameAr = request.NameAr,
                Description = request.Description,
                IsActive = request.IsActive
            };

            _context.HotelCities.Add(city);
            await _context.SaveChangesAsync();

            return MapToDto(city);
        }

        public async Task<bool> UpdateAsync(int id, HotelCityRequestDto request)
        {
            var city = await _context.HotelCities.FirstOrDefaultAsync(c => c.Id == id);
            if (city is null)
            {
                return false;
            }

            city.Name = request.Name;
            city.NameAr = request.NameAr;
            city.Description = request.Description;
            city.IsActive = request.IsActive;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var city = await _context.HotelCities.FirstOrDefaultAsync(c => c.Id == id);
            if (city is null)
            {
                return false;
            }

            var hasHotels = await _context.Hotels.AnyAsync(h => h.HotelCityId == id);
            if (hasHotels)
            {
                throw new InvalidOperationException("لا يمكن حذف المدينة لأنها مرتبطة بفنادق.");
            }

            _context.HotelCities.Remove(city);
            await _context.SaveChangesAsync();
            return true;
        }

        private static HotelCityResponseDto MapToDto(HotelCity city) => new()
        {
            Id = city.Id,
            Name = city.Name,
            NameAr = city.NameAr,
            Description = city.Description,
            IsActive = city.IsActive,
            HotelsCount = city.Hotels?.Count ?? 0,
            CreatedAt = city.CreatedAt
        };
    }
}
