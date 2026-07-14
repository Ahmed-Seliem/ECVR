using System.ComponentModel.DataAnnotations;

namespace ECM.ReservationSystem.Models.DTOs.HotelTrips
{
    public class HotelTripRequestDto
    {
        [Required(ErrorMessage = "يجب اختيار الفندق")]
        [Range(1, int.MaxValue, ErrorMessage = "يجب اختيار الفندق")]
        public int? HotelId { get; set; }

        [Required(ErrorMessage = "تاريخ البداية مطلوب")]
        public DateTime? StartDate { get; set; }

        [Required(ErrorMessage = "تاريخ النهاية مطلوب")]
        public DateTime? EndDate { get; set; }

        [StringLength(500, ErrorMessage = "الملاحظات يجب ألا تتجاوز 500 حرف")]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
