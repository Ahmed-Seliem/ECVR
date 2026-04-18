using System.ComponentModel.DataAnnotations;
using ECM.ReservationSystem.Domain.Common;

namespace ECM.ReservationSystem.Domain.Entities;

public class UnitType : AuditableEntity
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string NameAr { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    public bool IsForManagement { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Unit> Units { get; set; } = new List<Unit>();
}
