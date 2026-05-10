namespace ECM.ReservationSystem.Models.DTOs
{
    public class ReservationHoldResponseDto
    {
        public int HoldId { get; set; }
        public string HoldToken { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public int UnitId { get; set; }
        public int CityId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
