namespace ECM.ReservationSystem.Models.DTOs.OneDayTrips
{
    public class TripLocationResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int TripsCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
