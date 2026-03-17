using ECM.ReservationSystem.Domain.Entities;

namespace ECM.ReservationSystem.Services.Interfaces
{
    public interface IPricingService
    {
        Task<Pricing> GetCurrentPricingAsync(int unitId, FloorType floorType);
        Task<decimal> CalculateWeeklyRentAsync(int unitId, FloorType floorType, int numberOfGuests);
        Task<decimal> GetTransportationCostAsync(int cityId, int unitId, FloorType floorType, int numberOfGuests = 1);
        Task<decimal> CalculateTotalCostAsync(int unitId, FloorType floorType, int numberOfGuests, bool includeTransportation);
    }
}
