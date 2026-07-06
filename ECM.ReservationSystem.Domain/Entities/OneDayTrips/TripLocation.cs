using System.ComponentModel.DataAnnotations;
using ECM.ReservationSystem.Domain.Common;

namespace ECM.ReservationSystem.Domain.Entities.OneDayTrips;

public class TripLocation : AuditableEntity
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string NameAr { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
