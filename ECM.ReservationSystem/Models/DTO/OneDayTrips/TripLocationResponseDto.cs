namespace ECM.ReservationSystem.Models.DTOs.OneDayTrips
{
    public class TripLocationResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal AdultTicketPrice { get; set; }
        public decimal ChildTicketPrice { get; set; }
        public decimal CompanionTicketPrice { get; set; }
        public int EmployeeTicketCount { get; set; }
        public int PensionTicketCount { get; set; }
        public int EmployeeRemaining { get; set; }
        public int PensionRemaining { get; set; }
        public bool IsActive { get; set; }
        public int TripsCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
