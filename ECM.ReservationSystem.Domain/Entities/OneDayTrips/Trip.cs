using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ECM.ReservationSystem.Domain.Common;

namespace ECM.ReservationSystem.Domain.Entities.OneDayTrips;

public class Trip : AuditableEntity
{
    public int TripLocationId { get; set; }
    public TripLocation TripLocation { get; set; } = null!;

    [Required]
    public DateTime TripDate { get; set; }

    // Ticket prices moved to the TripLocation (prices are defined per place, not per trip).

    [StringLength(500)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<TripBooking> Bookings { get; set; } = new List<TripBooking>();
}
