using ECM.ReservationSystem.Models.DTOs.OneDayTrips;

namespace ECM.ReservationSystem.Services.OneDayTrips.Interfaces
{
    public interface ITripService
    {
        Task<List<TripResponseDto>> GetAllAsync(int? locationId = null);
        Task<TripResponseDto?> GetByIdAsync(int id);
        Task<TripResponseDto> CreateAsync(TripRequestDto request);
        Task<bool> UpdateAsync(int id, TripRequestDto request);
        Task<bool> DeleteAsync(int id);
    }
}
