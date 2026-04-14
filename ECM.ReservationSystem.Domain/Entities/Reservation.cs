using ECM.ReservationSystem.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECM.ReservationSystem.Domain.Entities;

public class Reservation : AuditableEntity
{
    [Required]
    [StringLength(100)]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string EmployeeName { get; set; } = string.Empty;

    [Required]
    public string Year { get; set; } = string.Empty;

    [Required]
    public DateTime CheckInDate { get; set; }

    [Required]
    public DateTime CheckOutDate { get; set; }

    public int NumberOfGuests { get; set; }
    public string CaseSystemId { get; set; } = string.Empty;
    public long? DocumentId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal WeeklyRent { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal InsuranceAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TransportationCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.TemporaryHold;
    public DateTime? PaymentDeadline { get; set; }
    public bool IsTransportationRequired { get; set; }

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;

    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;
}

public enum ReservationStatus
{
    TemporaryHold = 1,
    Confirmed = 2,
    Paid = 3,
    Cancelled = 4,
    Approved = 5
}
