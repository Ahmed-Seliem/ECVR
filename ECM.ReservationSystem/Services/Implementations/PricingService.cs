using Microsoft.EntityFrameworkCore;
using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities;
using ECM.ReservationSystem.Services.Interfaces;

namespace ECM.ReservationSystem.Services.Implementations
{
    public class PricingService : IPricingService
    {
        private readonly ApplicationDbContext _context;

        public PricingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Pricing> GetCurrentPricingAsync(int unitId, FloorType floorType)
        {
            var currentDate = DateTime.Now.Date;

            return await _context.Pricings
                .Where(p => p.UnitId == unitId
                           && p.FloorType == floorType
                           && p.IsActive
                           && p.EffectiveFrom <= currentDate
                           && (p.EffectiveTo == null || p.EffectiveTo >= currentDate))
                .OrderByDescending(p => p.EffectiveFrom)
                .FirstOrDefaultAsync();
        }

        public async Task<decimal> CalculateWeeklyRentAsync(int unitId, FloorType floorType, int numberOfGuests)
        {
            var pricing = await GetCurrentPricingAsync(unitId, floorType);
            if (pricing == null)
                return 0;

            return pricing.CalculateWeeklyRent(numberOfGuests);
        }

        public async Task<decimal> GetTransportationCostAsync(int cityId)
        {
            var currentDate = DateTime.Now.Date;

            var transportationCost = await _context.TransportationCosts
                .Where(tc => tc.CityId == cityId
                            && tc.IsActive
                            && tc.EffectiveFrom <= currentDate
                            && (tc.EffectiveTo == null || tc.EffectiveTo >= currentDate))
                .OrderByDescending(tc => tc.EffectiveFrom)
                .FirstOrDefaultAsync();

            return transportationCost?.RoundTripCost ?? 0;
        }

        public async Task<decimal> CalculateTotalCostAsync(int unitId, FloorType floorType, int numberOfGuests, bool includeTransportation)
        {
            var pricing = await GetCurrentPricingAsync(unitId, floorType);
            if (pricing == null)
                return 0;

            var weeklyRent = pricing.CalculateWeeklyRent(numberOfGuests);
            var insuranceAmount = pricing.InsuranceAmount;
            var transportationCost = 0m;

            if (includeTransportation)
            {
                var unit = await _context.Units.FindAsync(unitId);
                if (unit != null)
                {
                    transportationCost = await GetTransportationCostAsync(unit.CityId);
                }
            }

            return weeklyRent + insuranceAmount + transportationCost;
        }
    }
}