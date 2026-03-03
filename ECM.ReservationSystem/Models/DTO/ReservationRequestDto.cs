using System.ComponentModel.DataAnnotations;

namespace ECM.ReservationSystem.Models.DTOs
{
    public class ReservationRequestDto
    {
        [Required]
        public string EmployeeNumber { get; set; }

        [Required]
        public string EmployeeName { get; set; }

        [Required]
        public int UnitId { get; set; }

        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        [Required]
        [Range(1, 6)]
        public int NumberOfGuests { get; set; }

        public bool IsTransportationRequired { get; set; }

        public string Notes { get; set; }

        public string CaseSystemId { get; set; }
    }
}
