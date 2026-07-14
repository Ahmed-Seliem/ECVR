using System.ComponentModel.DataAnnotations;

namespace ECM.ReservationSystem.Models.DTOs.HotelTrips
{
    public class HotelRequestDto
    {
        [Required(ErrorMessage = "يجب اختيار المدينة")]
        [Range(1, int.MaxValue, ErrorMessage = "يجب اختيار المدينة")]
        public int? HotelCityId { get; set; }

        [Required(ErrorMessage = "اسم الفندق مطلوب")]
        [StringLength(100, ErrorMessage = "اسم الفندق يجب ألا يتجاوز 100 حرف")]
        public string Name { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "الاسم بالعربية يجب ألا يتجاوز 100 حرف")]
        public string NameAr { get; set; } = string.Empty;

        [Required(ErrorMessage = "سعر تذكرة البالغ مطلوب")]
        [Range(0, 9999999.99, ErrorMessage = "سعر تذكرة البالغ يجب أن يكون رقمًا موجبًا")]
        public decimal? AdultTicketPrice { get; set; }

        [Required(ErrorMessage = "سعر تذكرة الطفل مطلوب")]
        [Range(0, 9999999.99, ErrorMessage = "سعر تذكرة الطفل يجب أن يكون رقمًا موجبًا")]
        public decimal? ChildTicketPrice { get; set; }

        [Required(ErrorMessage = "سعر تذكرة المرافق مطلوب")]
        [Range(0, 9999999.99, ErrorMessage = "سعر تذكرة المرافق يجب أن يكون رقمًا موجبًا")]
        public decimal? CompanionTicketPrice { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "عدد التذاكر يجب أن يكون رقمًا موجبًا")]
        public int TicketQuantity { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
