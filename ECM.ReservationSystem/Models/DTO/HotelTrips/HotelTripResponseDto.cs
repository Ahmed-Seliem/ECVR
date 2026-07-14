namespace ECM.ReservationSystem.Models.DTOs.HotelTrips
{
    public class HotelTripResponseDto
    {
        public int Id { get; set; }
        public int HotelId { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string CityName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public int BookingsCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
