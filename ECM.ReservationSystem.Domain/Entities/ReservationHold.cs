using ECM.ReservationSystem.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace ECM.ReservationSystem.Domain.Entities;

public class ReservationHold : AuditableEntity
{
    [Required]
    [StringLength(64)]
    public string HoldToken { get; set; } = string.Empty;

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

    [Required]
    public int UnitId { get; set; }

    [Required]
    public int CityId { get; set; }

    [Required]
    public DateTime CheckInDate { get; set; }

    [Required]
    public DateTime CheckOutDate { get; set; }

    public int NumberOfGuests { get; set; }

    public bool IsTransportationRequired { get; set; }

    [StringLength(100)]
    public string CaseSystemId { get; set; } = string.Empty;

    public long? WorkflowId { get; set; }

    public long? DocumentId { get; set; }

    [Required]
    public DateTime ExpiresAt { get; set; }

    public DateTime? ReleasedAt { get; set; }

    [StringLength(200)]
    public string ReleaseReason { get; set; } = string.Empty;

    public Unit Unit { get; set; } = null!;
}
