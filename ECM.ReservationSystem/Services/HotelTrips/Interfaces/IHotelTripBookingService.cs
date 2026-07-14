using ECM.ReservationSystem.Models.DTOs.HotelTrips;

namespace ECM.ReservationSystem.Services.HotelTrips.Interfaces
{
    public interface IHotelTripBookingService
    {
        Task<List<HotelTripBookingResponseDto>> GetAllAsync(int? hotelTripId = null, Domain.Entities.HotelTrips.HotelBookingType? bookingType = null);
        Task<HotelTripBookingResponseDto?> GetByIdAsync(int id);
        Task<List<HotelTripBookingResponseDto>> GetByEmployeeAsync(string employeeNumber);
        Task<HotelTripBookingResponseDto> CreateAsync(HotelTripBookingRequestDto request);
        Task<bool> ConfirmPaymentAsync(int id);
        Task<bool> CancelAsync(int id);
        Task<bool> SetStatusAsync(int id, Domain.Entities.HotelTrips.HotelBookingStatus status);
        Task<bool> UpdateStatusByDocumentIdAsync(long documentId, Domain.Entities.HotelTrips.HotelBookingStatus status);
        Task<int> CancelExpiredAsync();
    }
}
