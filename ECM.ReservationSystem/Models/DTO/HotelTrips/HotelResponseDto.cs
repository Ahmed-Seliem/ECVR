namespace ECM.ReservationSystem.Models.DTOs.HotelTrips
{
    public class HotelResponseDto
    {
        public int Id { get; set; }
        public int HotelCityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public decimal AdultTicketPrice { get; set; }
        public decimal ChildTicketPrice { get; set; }
        public decimal CompanionTicketPrice { get; set; }
        public int TicketQuantity { get; set; }
        public int RemainingTickets { get; set; }
        public bool IsActive { get; set; }
        public int TripsCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
