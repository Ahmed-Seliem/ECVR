using System.ComponentModel.DataAnnotations.Schema;
using ECM.ReservationSystem.Domain.Common;

namespace ECM.ReservationSystem.Domain.Entities;

public class UnitScheduleSlot : AuditableEntity
{
    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    public int Year { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime SlotStartDate { get; set; }
    public DateTime SlotEndDate { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? WeeklyRentOverride { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsPaid { get; set; }
    public string? Notes { get; set; }

    [NotMapped]
    public bool IsBooked { get; set; }
}
