using ECM.ReservationSystem.Models.DTOs.HotelTrips;

namespace ECM.ReservationSystem.Services.HotelTrips.Interfaces
{
    public interface IHotelService
    {
        Task<List<HotelResponseDto>> GetAllAsync(int? cityId = null);
        Task<HotelResponseDto?> GetByIdAsync(int id);
        Task<int> GetRemainingTicketsAsync(int hotelId);
        Task<HotelResponseDto> CreateAsync(HotelRequestDto request);
        Task<bool> UpdateAsync(int id, HotelRequestDto request);
        Task<bool> DeleteAsync(int id);
    }
}
