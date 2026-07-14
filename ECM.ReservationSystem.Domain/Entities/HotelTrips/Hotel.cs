using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ECM.ReservationSystem.Domain.Common;

namespace ECM.ReservationSystem.Domain.Entities.HotelTrips;

public class Hotel : AuditableEntity
{
    public int HotelCityId { get; set; }
    public HotelCity HotelCity { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string NameAr { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal AdultTicketPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ChildTicketPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CompanionTicketPrice { get; set; }

    public bool IsActive { get; set; } = true;

    // Ticket pool for this hotel (shared across all its trips).
    public Ticket? Ticket { get; set; }

    public ICollection<HotelTrip> HotelTrips { get; set; } = new List<HotelTrip>();
}
