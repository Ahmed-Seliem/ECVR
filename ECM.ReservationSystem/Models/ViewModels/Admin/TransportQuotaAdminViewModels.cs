using System.ComponentModel.DataAnnotations;
using ECM.ReservationSystem.Domain.Entities;

namespace ECM.ReservationSystem.Models.ViewModels.Admin
{
    public class TransportQuotaIndexViewModel : TransportQuotaFormViewModel
    {
        public List<TransportQuota> Items { get; set; } = new();
    }

    public class TransportQuotaFormViewModel
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "اختر المدينة.")]
        public int CityId { get; set; }

        [Range(2000, 3000, ErrorMessage = "أدخل سنة موسم صحيحة.")]
        public int SeasonYear { get; set; } = DateTime.Now.Year;

        [Range(1, 100, ErrorMessage = "عدد الأتوبيسات يجب أن يكون بين 1 و100.")]
        public int BusCount { get; set; }

        [Range(1, 100, ErrorMessage = "عدد المقاعد يجب أن يكون بين 1 و100.")]
        public int SeatsPerBus { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;
    }

    public class WeeklyReservationReportGroupViewModel
    {
        public string CityName { get; set; } = string.Empty;
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public int ReservationCount { get; set; }
        public int PassengerCount { get; set; }
        public List<WeeklyReservationReportItemViewModel> Reservations { get; set; } = new();
    }

    public class WeeklyReservationReportItemViewModel
    {
        public int ReservationId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public int NumberOfGuests { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
