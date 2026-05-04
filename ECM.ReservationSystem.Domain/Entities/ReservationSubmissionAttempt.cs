using System.ComponentModel.DataAnnotations;
using ECM.ReservationSystem.Domain.Common;

namespace ECM.ReservationSystem.Domain.Entities;

public class ReservationSubmissionAttempt : AuditableEntity
{
    [Required]
    [StringLength(50)]
    public string Outcome { get; set; } = string.Empty;

    public int? ReservationId { get; set; }
    public long? DocumentId { get; set; }
    public long? WorkflowId { get; set; }

    [StringLength(100)]
    public string EmployeeNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string EmployeeName { get; set; } = string.Empty;

    [StringLength(200)]
    public string Sector { get; set; } = string.Empty;

    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    public int UnitId { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int NumberOfGuests { get; set; }
    public bool IsTransportationRequired { get; set; }

    [StringLength(100)]
    public string PaymentReceiptNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string InsuranceReceiptNumber { get; set; } = string.Empty;

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;

    public string CaseSystemId { get; set; } = string.Empty;
    public string RequestPayload { get; set; } = string.Empty;
    public string ResponsePayload { get; set; } = string.Empty;

    [StringLength(2000)]
    public string FailureReason { get; set; } = string.Empty;
}
