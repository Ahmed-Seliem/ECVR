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

    // Separate ticket pools per booking type, shared across all the location's trips (1 ticket per person).
    public int EmployeeTicketCount { get; set; }
    public int PensionTicketCount { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
