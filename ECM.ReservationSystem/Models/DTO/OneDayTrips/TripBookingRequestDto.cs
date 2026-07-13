using System.ComponentModel.DataAnnotations;

namespace ECM.ReservationSystem.Models.DTOs.OneDayTrips
{
    public class TripBookingRequestDto
    {
        [Required(ErrorMessage = "يجب اختيار الرحلة")]
        [Range(1, int.MaxValue, ErrorMessage = "يجب اختيار الرحلة")]
        public int TripId { get; set; }

        [Required(ErrorMessage = "رقم الموظف مطلوب")]
        [StringLength(100, ErrorMessage = "رقم الموظف يجب ألا يتجاوز 100 حرف")]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم الموظف مطلوب")]
        [StringLength(100, ErrorMessage = "اسم الموظف يجب ألا يتجاوز 100 حرف")]
        public string EmployeeName { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "القطاع يجب ألا يتجاوز 200 حرف")]
        public string Sector { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "رقم الهاتف يجب ألا يتجاوز 20 رقمًا")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Range(1, 5, ErrorMessage = "عدد البالغين يجب أن يكون بين 1 و 5")]
        public int AdultsCount { get; set; }

        [Range(0, 4, ErrorMessage = "عدد الأطفال يجب أن يكون بين 0 و 4")]
        public int ChildrenCount { get; set; }

        [Range(0, 4, ErrorMessage = "عدد المرافقين يجب أن يكون بين 0 و 4")]
        public int CompanionsCount { get; set; }

        // From the form: "employees" or "pensions". Pension adds a surcharge on the total.
        public string BookingType { get; set; } = "employees";

        public string CaseSystemId { get; set; } = string.Empty;
        public long? WorkflowId { get; set; }
        public long? DocumentId { get; set; }

        [StringLength(500, ErrorMessage = "الملاحظات يجب ألا تتجاوز 500 حرف")]
        public string? Notes { get; set; }
    }
}
