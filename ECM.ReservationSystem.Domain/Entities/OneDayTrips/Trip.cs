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

    [Column(TypeName = "decimal(18,2)")]
    public decimal AdultTicketPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ChildTicketPrice { get; set; }

    public TripStatus Status { get; set; } = TripStatus.Open;

    [StringLength(500)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<TripBooking> Bookings { get; set; } = new List<TripBooking>();
}

public enum TripStatus
{
    Open = 1,
    Closed = 2,
    Cancelled = 3
}
