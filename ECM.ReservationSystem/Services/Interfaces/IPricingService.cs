using ECM.ReservationSystem.Domain.Entities;

namespace ECM.ReservationSystem.Services.Interfaces
{
    public interface IPricingService
    {
        Task<Pricing?> GetCurrentPricingAsync(int unitId);
        Task<decimal> CalculateWeeklyRentAsync(int unitId, int numberOfGuests);
        Task<decimal> GetTransportationCostAsync(int cityId, int unitId, int numberOfGuests = 1);
        Task<decimal> CalculateTotalCostAsync(int unitId, int numberOfGuests, bool includeTransportation);
    }
}
