namespace ECM.ReservationSystem.Models.DTOs
{
    public class CostCalculationDto
    {
        public int UnitId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NumberOfGuests { get; set; }
        public bool IsTransportationRequired { get; set; }

        // Response fields
        public decimal WeeklyRent { get; set; }
        public decimal InsuranceAmount { get; set; }
        public decimal TransportationCost { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsAvailable { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
