using System.ComponentModel.DataAnnotations.Schema;
using ECM.ReservationSystem.Domain.Common;

namespace ECM.ReservationSystem.Domain.Entities;

public class TransportationCost : AuditableEntity
{
    [Column(TypeName = "decimal(18,2)")]
    public decimal RoundTripCost { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;

    public int CityId { get; set; }
    public City City { get; set; } = null!;
}
