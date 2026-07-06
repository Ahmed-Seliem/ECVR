using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ECM.ReservationSystem.Domain.Common;

namespace ECM.ReservationSystem.Domain.Entities.OneDayTrips;

public class TripBooking : AuditableEntity
{
    public int TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string EmployeeName { get; set; } = string.Empty;

    [StringLength(200)]
    public string Sector { get; set; } = string.Empty;

    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    public int AdultsCount { get; set; }
    public int ChildrenCount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AdultUnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ChildUnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;

    public string CaseSystemId { get; set; } = string.Empty;
    public long? WorkflowId { get; set; }
    public long? DocumentId { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public enum BookingStatus
{
    Confirmed = 1,
    Cancelled = 2
}
