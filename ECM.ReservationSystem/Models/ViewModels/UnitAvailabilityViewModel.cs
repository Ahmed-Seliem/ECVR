using ECM.ReservationSystem.Domain.Entities;

namespace ECM.ReservationSystem.Models.ViewModels.Admin
{
    public class UnitAvailabilityViewModel
    {
        public int UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string CityName { get; set; } = string.Empty;
        public string UnitTypeName { get; set; } = string.Empty;
        public FloorType FloorType { get; set; }
        public int Year { get; set; }
        public bool IsForPensioners { get; set; }
        public bool IsForManagement { get; set; }
        public List<WeekAvailabilityViewModel> WeekAvailabilities { get; set; } = new();
    }

    public class WeekAvailabilityViewModel
    {
        public int? SlotId { get; set; }
        public string SlotName { get; set; } = string.Empty;
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsReserved { get; set; }
        public bool IsScheduled { get; set; }
        public string ReservationStatus { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class UnitsAvailabilityPageViewModel
    {
        public int Year { get; set; }
        public int? CityId { get; set; }
        public int? UnitTypeId { get; set; }
        public int? UnitId { get; set; }
        public bool? IsForManagement { get; set; }
        public List<UnitAvailabilityViewModel> Units { get; set; } = new();
    }
}
