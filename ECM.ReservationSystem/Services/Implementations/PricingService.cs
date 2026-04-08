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

        public async Task<Pricing?> GetCurrentPricingAsync(int unitId)
        {
            var currentDate = DateTime.Now.Date;

            return await _context.Pricings
                .Where(p => p.UnitId == unitId
                           && p.IsActive
                           && p.EffectiveFrom <= currentDate
                           && (p.EffectiveTo == null || p.EffectiveTo >= currentDate))
                .OrderByDescending(p => p.EffectiveFrom)
                .FirstOrDefaultAsync();
        }

        public async Task<decimal> CalculateWeeklyRentAsync(int unitId, int numberOfGuests)
        {
            var pricing = await GetCurrentPricingAsync(unitId);
            return pricing?.WeeklyRentDefaultCapacity ?? 0;
        }

        public async Task<decimal> GetTransportationCostAsync(int cityId, int unitId, int numberOfGuests = 1)
        {
            var pricing = await GetCurrentPricingAsync(unitId);
            if (pricing == null || pricing.TransportationCostPerPerson <= 0)
            {
                return 0;
            }

            return pricing.TransportationCostPerPerson * Math.Max(1, numberOfGuests);
        }

        public async Task<decimal> CalculateTotalCostAsync(int unitId, int numberOfGuests, bool includeTransportation)
        {
            var pricing = await GetCurrentPricingAsync(unitId);
            if (pricing == null)
            {
                return 0;
            }

            var weeklyRent = pricing.WeeklyRentDefaultCapacity;
            var insuranceAmount = pricing.InsuranceAmount;
            var transportationCost = includeTransportation
                ? await GetTransportationCostAsync(0, unitId, numberOfGuests)
                : 0;

            return weeklyRent + insuranceAmount + transportationCost;
        }
    }
}
