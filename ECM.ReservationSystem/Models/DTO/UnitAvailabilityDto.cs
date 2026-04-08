namespace ECM.ReservationSystem.Models.DTOs
{
    public class UnitAvailabilityDto
    {
        public int UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string CityName { get; set; } = string.Empty;
        public string UnitTypeName { get; set; } = string.Empty;
        public string UnitNumber { get; set; } = string.Empty;
        public string FacadeName { get; set; } = string.Empty;
        public int FloorNumber { get; set; }
        public int Capacity { get; set; }
        public int RoomCount { get; set; }
        public int Year { get; set; }
        public bool IsForPensioners { get; set; }
        public bool IsForManagement { get; set; }
        public decimal WeeklyRentDefaultCapacity { get; set; }
        public decimal InsuranceAmount { get; set; }
        public decimal TransportationCostPerPerson { get; set; }
        public bool IsAvailable { get; set; }
        public List<AvailableWeekDto> AvailableWeeks { get; set; } = new();
    }

    public class AvailableWeekDto
    {
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public decimal TotalCost { get; set; }
        public bool IsBookingOpen { get; set; }
    }
}
