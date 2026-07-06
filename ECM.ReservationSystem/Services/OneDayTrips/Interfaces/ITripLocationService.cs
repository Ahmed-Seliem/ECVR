using ECM.ReservationSystem.Models.DTOs.OneDayTrips;

namespace ECM.ReservationSystem.Services.OneDayTrips.Interfaces
{
    public interface ITripLocationService
    {
        Task<List<TripLocationResponseDto>> GetAllAsync(bool includeInactive = true);
        Task<TripLocationResponseDto?> GetByIdAsync(int id);
        Task<TripLocationResponseDto> CreateAsync(TripLocationRequestDto request);
        Task<bool> UpdateAsync(int id, TripLocationRequestDto request);
        Task<bool> DeleteAsync(int id);
    }
}
