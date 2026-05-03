using ECM.ReservationSystem.Domain.Entities;

namespace ECM.ReservationSystem.Models.ViewModels.Admin;

public class ReportsIndexViewModel
{
    public int Year { get; set; } = DateTime.Now.Year;
    public int? CityId { get; set; }
    public int? UnitTypeId { get; set; }
    public string? WeekId { get; set; }
    public ReservationStatus? Status { get; set; }
    public List<ReportsReservationGroupViewModel> Groups { get; set; } = new();
}

public class ReportsReservationGroupViewModel
{
    public string CityName { get; set; } = string.Empty;
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public int ReservationCount { get; set; }
    public int PassengerCount { get; set; }
    public decimal TotalAmount { get; set; }
    public List<ReportsReservationItemViewModel> Items { get; set; } = new();
}

public class ReportsReservationItemViewModel
{
    public int ReservationId { get; set; }
    public string FloorDisplayName { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string InsuranceReceiptNumber { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string UnitCode { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string UnitTypeName { get; set; } = string.Empty;
    public int NumberOfGuests { get; set; }
    public bool IsTransportationRequired { get; set; }
    public decimal UnitAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}
