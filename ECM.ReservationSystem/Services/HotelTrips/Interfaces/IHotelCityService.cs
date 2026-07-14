using ECM.ReservationSystem.Models.DTOs.HotelTrips;

namespace ECM.ReservationSystem.Services.HotelTrips.Interfaces
{
    public interface IHotelCityService
    {
        Task<List<HotelCityResponseDto>> GetAllAsync(bool includeInactive = true);
        Task<HotelCityResponseDto?> GetByIdAsync(int id);
        Task<HotelCityResponseDto> CreateAsync(HotelCityRequestDto request);
        Task<bool> UpdateAsync(int id, HotelCityRequestDto request);
        Task<bool> DeleteAsync(int id);
    }
}
