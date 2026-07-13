using ECM.ReservationSystem.Models.DTOs.OneDayTrips;

namespace ECM.ReservationSystem.Services.OneDayTrips.Interfaces
{
    public interface ITripBookingService
    {
        Task<List<TripBookingResponseDto>> GetAllAsync(int? tripId = null, Domain.Entities.OneDayTrips.TripBookingType? bookingType = null);
        Task<TripBookingResponseDto?> GetByIdAsync(int id);
        Task<List<TripBookingResponseDto>> GetByEmployeeAsync(string employeeNumber);
        Task<TripBookingResponseDto> CreateAsync(TripBookingRequestDto request);
        Task<bool> ConfirmPaymentAsync(int id);
        Task<bool> CancelAsync(int id);
        // Admin override: set a booking to any status directly.
        Task<bool> SetStatusAsync(int id, Domain.Entities.OneDayTrips.BookingStatus status);
        // Update a booking's status by its workflow DocumentId (used by the Case WF code activity).
        Task<bool> UpdateStatusByDocumentIdAsync(long documentId, Domain.Entities.OneDayTrips.BookingStatus status);
        // Auto-cancel pending-payment bookings whose deadline has passed; returns how many were released.
        Task<int> CancelExpiredAsync();
    }
}
