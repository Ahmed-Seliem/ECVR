using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECM.ReservationSystem.Models.Entities
{
    public class Reservation
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string EmployeeNumber { get; set; }

        [Required]
        [StringLength(100)]
        public string EmployeeName { get; set; }
        [Required]
        public string Year { get; set; }
        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        public int NumberOfGuests { get; set; }

        public string CaseSystemId { get; set; } // للربط مع نظام CASE

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
        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        public int UnitId { get; set; }

        // Navigation Properties
        public Unit Unit { get; set; }
    }

    public enum ReservationStatus
    {
        TemporaryHold = 1,      // حجز مؤقت
        Confirmed = 2,          // مؤكد
        Paid = 3,              // مدفوع
        Cancelled = 4,          // ملغي
        Approved = 5           // مكتمل
    }
}