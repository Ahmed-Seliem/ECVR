using ECM.ReservationSystem.Domain.Entities.OneDayTrips;

namespace ECM.ReservationSystem.Models.DTOs.OneDayTrips
{
    public class TripBookingResponseDto
    {
        public int Id { get; set; }
        public int TripId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public DateTime TripDate { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int AdultsCount { get; set; }
        public int ChildrenCount { get; set; }
        public int CompanionsCount { get; set; }
        public decimal AdultUnitPrice { get; set; }
        public decimal ChildUnitPrice { get; set; }
        public decimal CompanionUnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
        public TripBookingType BookingType { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime? PaymentDeadline { get; set; }
        public string CaseSystemId { get; set; } = string.Empty;
        public long? WorkflowId { get; set; }
        public long? DocumentId { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
