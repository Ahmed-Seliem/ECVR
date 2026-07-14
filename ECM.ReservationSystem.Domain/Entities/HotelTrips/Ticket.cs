using ECM.ReservationSystem.Domain.Common;

namespace ECM.ReservationSystem.Domain.Entities.HotelTrips;

// Ticket pools for a hotel, shared across all its trips. Separate pools per booking type. 1 ticket per person.
public class Ticket : AuditableEntity
{
    public int HotelId { get; set; }
    public Hotel Hotel { get; set; } = null!;

    public int EmployeeQuantity { get; set; }
    public int PensionQuantity { get; set; }
}
