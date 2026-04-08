using System.ComponentModel.DataAnnotations;
using ECM.ReservationSystem.Domain.Common;

namespace ECM.ReservationSystem.Domain.Entities;

public class UnitFacade : AuditableEntity
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? NameAr { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Unit> Units { get; set; } = new List<Unit>();
}
