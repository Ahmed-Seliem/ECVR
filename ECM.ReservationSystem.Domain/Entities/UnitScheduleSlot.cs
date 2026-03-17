using System.ComponentModel.DataAnnotations.Schema;
using ECM.ReservationSystem.Domain.Common;

namespace ECM.ReservationSystem.Domain.Entities;

public class UnitScheduleSlot : AuditableEntity
{
    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    public int Year { get; set; }
    public DateTime SlotStartDate { get; set; }
    public DateTime SlotEndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    [NotMapped]
    public bool IsBooked { get; set; }
}
