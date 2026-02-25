using System.ComponentModel.DataAnnotations;

namespace ECM.ReservationSystem.Models.Entities
{
    public class AvailableDate
    {
        public int Id { get; set; }

        public DateTime AvailableFrom { get; set; }

        public DateTime AvailableTo { get; set; }

        public bool IsBookingOpen { get; set; } = true; // هل باب الحجز مفتوح؟

        public DateTime BookingOpenFrom { get; set; }

        public DateTime BookingOpenTo { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Foreign Keys
        public int CityId { get; set; }

        // Navigation Properties
        public City City { get; set; }
    }
}