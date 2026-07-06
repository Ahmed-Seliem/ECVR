using ECM.ReservationSystem.Models.DTOs.OneDayTrips;

namespace ECM.ReservationSystem.Services.OneDayTrips.Interfaces
{
    public interface ITripBookingService
    {
        Task<List<TripBookingResponseDto>> GetAllAsync(int? tripId = null);
        Task<TripBookingResponseDto?> GetByIdAsync(int id);
        Task<List<TripBookingResponseDto>> GetByEmployeeAsync(string employeeNumber);
        Task<TripBookingResponseDto> CreateAsync(TripBookingRequestDto request);
        Task<bool> CancelAsync(int id);
    }
}
