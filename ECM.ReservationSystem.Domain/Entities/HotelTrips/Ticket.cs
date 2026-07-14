using ECM.ReservationSystem.Domain.Common;

namespace ECM.ReservationSystem.Domain.Entities.HotelTrips;

// The ticket pool (quantity) for a hotel, shared across all its trips. 1 ticket per person.
public class Ticket : AuditableEntity
{
    public int HotelId { get; set; }
    public Hotel Hotel { get; set; } = null!;

    public int Quantity { get; set; }
}
