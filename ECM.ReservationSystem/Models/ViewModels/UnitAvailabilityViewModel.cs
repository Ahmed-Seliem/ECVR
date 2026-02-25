using ECM.ReservationSystem.Models.Entities;

namespace ECM.ReservationSystem.Models.ViewModels.Admin
{
    public class UnitAvailabilityViewModel
    {
        public int UnitId { get; set; }
        public string UnitName { get; set; }
        public string CityName { get; set; }
        public string UnitTypeName { get; set; }
        public FloorType FloorType { get; set; }
        public int Year { get; set; }

        public List<WeekAvailabilityViewModel> WeekAvailabilities { get; set; } = new();
    }

    public class WeekAvailabilityViewModel
    {
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsReserved { get; set; }
        public string ReservationStatus { get; set; }
        public string EmployeeName { get; set; }
        public decimal WeeklyRent { get; set; }
    }
}