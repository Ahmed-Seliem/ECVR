using ECM.ReservationSystem.Models.DTOs.HotelTrips;

namespace ECM.ReservationSystem.Services.HotelTrips.Interfaces
{
    public interface IHotelTripService
    {
        Task<List<HotelTripResponseDto>> GetAllAsync(int? hotelId = null, int? year = null, int? month = null);
        Task<HotelTripResponseDto?> GetByIdAsync(int id);
        Task<HotelTripResponseDto> CreateAsync(HotelTripRequestDto request);
        Task<bool> UpdateAsync(int id, HotelTripRequestDto request);
        Task<bool> DeleteAsync(int id);
    }
}
