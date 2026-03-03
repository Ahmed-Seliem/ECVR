using ECM.ReservationSystem.Domain.Entities;

namespace ECM.ReservationSystem.Services.Interfaces
{
    public interface IUnitService
    {
        Task<List<Unit>> GetAvailableUnitsAsync(int cityId, int year);
        Task<Unit> GetUnitByIdAsync(int unitId);
        Task<bool> IsUnitAvailableAsync(int unitId, DateTime checkInDate, DateTime checkOutDate);
        Task<List<Unit>> GetUnitsByCityAsync(int cityId);
    }
}
