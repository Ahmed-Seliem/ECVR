using ECM.ReservationSystem.Domain.Entities;

namespace ECM.ReservationSystem.Models.DTOs
{
    public class ReservationResponseDto
    {
        public int ReservationId { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public string CityName { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NumberOfGuests { get; set; }
        public decimal WeeklyRent { get; set; }
        public decimal InsuranceAmount { get; set; }
        public decimal TransportationCost { get; set; }
        public decimal TotalAmount { get; set; }
        public ReservationStatus Status { get; set; }
        public DateTime? PaymentDeadline { get; set; }
        public bool IsTransportationRequired { get; set; }
        public string PaymentReceiptNumber { get; set; } = string.Empty;
        public string InsuranceReceiptNumber { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CaseSystemId { get; set; } = string.Empty;
        public long? DocumentId { get; set; }
    }
}
