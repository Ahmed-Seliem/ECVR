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

        // Ticket prices moved to the place (TripLocation).

        [StringLength(500, ErrorMessage = "الملاحظات يجب ألا تتجاوز 500 حرف")]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
