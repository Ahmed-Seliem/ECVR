using System.ComponentModel.DataAnnotations;
using ECM.ReservationSystem.Domain.Common;

namespace ECM.ReservationSystem.Domain.Entities;

public class TransportQuota : AuditableEntity
{
    public int CityId { get; set; }
    public City City { get; set; } = null!;

    public int SeasonYear { get; set; }

    [Range(1, 100)]
    public int BusCount { get; set; }

    [Range(1, 100)]
    public int SeatsPerBus { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;

    public int TotalSeats => BusCount * SeatsPerBus;
}
