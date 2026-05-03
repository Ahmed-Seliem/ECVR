using System.ComponentModel.DataAnnotations;

namespace ECM.ReservationSystem.Models.DTOs
{
    public class ReservationRequestDto
    {
        [Required]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required]
        public string EmployeeName { get; set; } = string.Empty;

        public string Sector { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(010|011|012|015)[0-9]{8}$")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public int UnitId { get; set; }

        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        [Required]
        [Range(1, 6)]
        public int NumberOfGuests { get; set; }

        public bool IsTransportationRequired { get; set; }

        public string PaymentReceiptNumber { get; set; } = string.Empty;

        public string InsuranceReceiptNumber { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public string CaseSystemId { get; set; } = string.Empty;
        public long? DocumentId { get; set; }
    }
}
