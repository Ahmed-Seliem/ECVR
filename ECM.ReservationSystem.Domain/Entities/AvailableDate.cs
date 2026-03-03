using ECM.ReservationSystem.Domain.Common;

namespace ECM.ReservationSystem.Domain.Entities;

public class AvailableDate : AuditableEntity
{
    public DateTime AvailableFrom { get; set; }
    public DateTime AvailableTo { get; set; }

    public bool IsBookingOpen { get; set; } = true;
    public DateTime BookingOpenFrom { get; set; }
    public DateTime BookingOpenTo { get; set; }
    public bool IsActive { get; set; } = true;

    public int CityId { get; set; }
    public City City { get; set; } = null!;
}
