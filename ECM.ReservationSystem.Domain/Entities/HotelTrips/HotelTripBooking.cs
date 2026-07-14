using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ECM.ReservationSystem.Domain.Common;

namespace ECM.ReservationSystem.Domain.Entities.HotelTrips;

public class HotelTripBooking : AuditableEntity
{
    public int HotelTripId { get; set; }
    public HotelTrip HotelTrip { get; set; } = null!;

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
    public int CompanionsCount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AdultUnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ChildUnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CompanionUnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    // Employee vs Pension. Pension bookings add a surcharge on top of the ticket total.
    public HotelBookingType BookingType { get; set; } = HotelBookingType.Employee;

    public HotelBookingStatus Status { get; set; } = HotelBookingStatus.PendingPayment;

    // Deadline to pay before the booking is auto-cancelled and its tickets are released.
    public DateTime? PaymentDeadline { get; set; }

    public string CaseSystemId { get; set; } = string.Empty;
    public long? WorkflowId { get; set; }
    public long? DocumentId { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public enum HotelBookingStatus
{
    PendingPayment = 1,
    Confirmed = 2,
    Cancelled = 3
}

public enum HotelBookingType
{
    Employee = 1,
    Pension = 2
}
