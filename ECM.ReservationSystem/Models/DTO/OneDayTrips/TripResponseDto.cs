namespace ECM.ReservationSystem.Models.DTOs.OneDayTrips
{
    public class TripResponseDto
    {
        public int Id { get; set; }
        public int TripLocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public DateTime TripDate { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public int BookingsCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
