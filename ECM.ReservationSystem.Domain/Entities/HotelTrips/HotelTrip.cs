using System.ComponentModel.DataAnnotations;
using ECM.ReservationSystem.Domain.Common;

namespace ECM.ReservationSystem.Domain.Entities.HotelTrips;

public class HotelTrip : AuditableEntity
{
    public int HotelId { get; set; }
    public Hotel Hotel { get; set; } = null!;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<HotelTripBooking> Bookings { get; set; } = new List<HotelTripBooking>();
}
