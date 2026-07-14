using System.ComponentModel.DataAnnotations;

namespace ECM.ReservationSystem.Models.DTOs.OneDayTrips
{
    public class TripLocationRequestDto
    {
        [Required(ErrorMessage = "الاسم بالإنجليزية مطلوب")]
        [StringLength(100, ErrorMessage = "الاسم بالإنجليزية يجب ألا يتجاوز 100 حرف")]
        public string Name { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "الاسم بالعربية يجب ألا يتجاوز 100 حرف")]
        public string NameAr { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "الوصف يجب ألا يتجاوز 500 حرف")]
        public string? Description { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "عدد تذاكر الموظفين يجب أن يكون رقمًا موجبًا")]
        public int EmployeeTicketCount { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "عدد تذاكر المعاشات يجب أن يكون رقمًا موجبًا")]
        public int PensionTicketCount { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
