using System.ComponentModel.DataAnnotations.Schema;
using ECM.ReservationSystem.Domain.Common;

namespace ECM.ReservationSystem.Domain.Entities;

public class Pricing : AuditableEntity
{
    [Column(TypeName = "decimal(18,2)")]
    public decimal WeeklyRentDefaultCapacity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AdditionalPersonCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal InsuranceAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TransportationCostPerPerson { get; set; }

    public FloorType FloorType { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;

    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    public decimal CalculateWeeklyRent(int numberOfGuests)
    {
        if (numberOfGuests <= 6)
        {
            return WeeklyRentDefaultCapacity;
        }

        var additionalGuests = numberOfGuests - 6;
        return WeeklyRentDefaultCapacity + (additionalGuests * AdditionalPersonCost);
    }
}
