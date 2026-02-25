using ECM.ReservationSystem.Models.Entities;

namespace ECM.ReservationSystem.Models.DTOs
{
    public class ReservationResponseDto
    {
        public int ReservationId { get; set; }
        public string EmployeeNumber { get; set; }
        public string EmployeeName { get; set; }
        public string UnitName { get; set; }
        public string CityName { get; set; }
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
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CaseSystemId { get; set; }
    }
}