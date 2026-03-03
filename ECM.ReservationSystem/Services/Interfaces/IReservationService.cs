using ECM.ReservationSystem.Models.DTOs;
using ECM.ReservationSystem.Domain.Entities;

namespace ECM.ReservationSystem.Services.Interfaces
{
    public interface IReservationService
    {
        Task<List<UnitAvailabilityDto>> GetAvailableUnitsAsync(int cityId, int year, DateTime? checkInDate = null, DateTime? checkOutDate = null);
        Task<CostCalculationDto> CalculateCostAsync(int unitId, DateTime checkInDate, DateTime checkOutDate, int numberOfGuests, bool isTransportationRequired);
        Task<ReservationResponseDto> CreateReservationAsync(ReservationRequestDto request);
        Task<bool> ConfirmReservationAsync(int reservationId);
        Task<bool> CancelReservationAsync(int reservationId);
        Task CleanupExpiredHoldsAsync();
        Task<ReservationResponseDto> GetReservationAsync(int reservationId);
        Task<List<ReservationResponseDto>> GetReservationsByEmployeeAsync(string employeeNumber);
    }
}