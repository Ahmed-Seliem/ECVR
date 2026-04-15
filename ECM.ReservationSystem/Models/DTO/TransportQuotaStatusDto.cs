namespace ECM.ReservationSystem.Models.DTOs
{
    public class TransportQuotaStatusDto
    {
        public int CityId { get; set; }
        public int SeasonYear { get; set; }
        public string WeekId { get; set; } = string.Empty;
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public int BusCount { get; set; }
        public int SeatsPerBus { get; set; }
        public int TotalSeats { get; set; }
        public int ReservedSeats { get; set; }
        public int RemainingSeats { get; set; }
        public bool HasCapacity => RemainingSeats > 0;
        public bool HasQuotaConfigured { get; set; }
    }
}
