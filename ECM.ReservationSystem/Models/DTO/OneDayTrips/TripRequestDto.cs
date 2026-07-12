using System.ComponentModel.DataAnnotations;

namespace ECM.ReservationSystem.Models.DTOs.OneDayTrips
{
    public class TripRequestDto
    {
        [Required(ErrorMessage = "يجب اختيار مكان الرحلة")]
        [Range(1, int.MaxValue, ErrorMessage = "يجب اختيار مكان الرحلة")]
        public int? TripLocationId { get; set; }

        [Required(ErrorMessage = "تاريخ الرحلة مطلوب")]
        public DateTime? TripDate { get; set; }

        [Required(ErrorMessage = "سعر تذكرة البالغ مطلوب")]
        [Range(0, 9999999.99, ErrorMessage = "سعر تذكرة البالغ يجب أن يكون رقمًا موجبًا")]
        public decimal? AdultTicketPrice { get; set; }

        [Required(ErrorMessage = "سعر تذكرة الطفل مطلوب")]
        [Range(0, 9999999.99, ErrorMessage = "سعر تذكرة الطفل يجب أن يكون رقمًا موجبًا")]
        public decimal? ChildTicketPrice { get; set; }

        [Required(ErrorMessage = "سعر تذكرة المرافق مطلوب")]
        [Range(0, 9999999.99, ErrorMessage = "سعر تذكرة المرافق يجب أن يكون رقمًا موجبًا")]
        public decimal? CompanionTicketPrice { get; set; }

        [StringLength(500, ErrorMessage = "الملاحظات يجب ألا تتجاوز 500 حرف")]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
