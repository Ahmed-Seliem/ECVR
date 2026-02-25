using ECM.ReservationSystem.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ECM.ReservationSystem.Models.ViewModels.Admin
{
    public class ReservationFilterViewModel
    {
        public ReservationStatus? Status { get; set; }
        public int? CityId { get; set; }
        public int? Year { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string EmployeeNumber { get; set; }

        // For dropdowns
        public SelectList Cities { get; set; }
        public SelectList Statuses { get; set; }
        public SelectList Years { get; set; }
    }
}

// Models/ViewModels/Admin/DashboardViewModel.cs
namespace ECM.ReservationSystem.Models.ViewModels.Admin
{
    public class DashboardViewModel
    {
        public int TotalCities { get; set; }
        public int TotalUnits { get; set; }
        public int TotalReservations { get; set; }
        public int PendingReservations { get; set; }
        public int ConfirmedReservations { get; set; }
        public int ExpiredTemporaryHolds { get; set; }

        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }

        // Recent activities
        public List<RecentReservationViewModel> RecentReservations { get; set; } = new();
        public List<ExpiringHoldViewModel> ExpiringHolds { get; set; } = new();

        // Statistics by city
        public List<CityStatisticsViewModel> CityStatistics { get; set; } = new();
    }

    public class RecentReservationViewModel
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; }
        public string UnitName { get; set; }
        public string CityName { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public ReservationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ExpiringHoldViewModel
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; }
        public string UnitName { get; set; }
        public DateTime PaymentDeadline { get; set; }
        public TimeSpan TimeRemaining { get; set; }
    }

    public class CityStatisticsViewModel
    {
        public string CityName { get; set; }
        public int TotalUnits { get; set; }
        public int AvailableUnits { get; set; }
        public int ReservedUnits { get; set; }
        public decimal Revenue { get; set; }
    }
}