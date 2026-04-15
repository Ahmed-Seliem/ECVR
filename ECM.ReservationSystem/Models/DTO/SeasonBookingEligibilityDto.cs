namespace ECM.ReservationSystem.Models.DTOs
{
    public class SeasonBookingEligibilityDto
    {
        public string EmployeeNumber { get; set; } = string.Empty;
        public int SeasonYear { get; set; }
        public bool HasExistingReservation { get; set; }
        public int? ReservationId { get; set; }
        public int? CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
