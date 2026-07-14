using ECM.ReservationSystem.Domain.Entities.HotelTrips;

namespace ECM.ReservationSystem.Models.DTOs.HotelTrips
{
    public class HotelTripBookingResponseDto
    {
        public int Id { get; set; }
        public int HotelTripId { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string CityName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
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
        public HotelBookingType BookingType { get; set; }
        public HotelBookingStatus Status { get; set; }
        public DateTime? PaymentDeadline { get; set; }
        public string CaseSystemId { get; set; } = string.Empty;
        public long? WorkflowId { get; set; }
        public long? DocumentId { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
