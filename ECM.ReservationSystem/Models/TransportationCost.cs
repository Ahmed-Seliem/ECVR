using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECM.ReservationSystem.Models.Entities
{
    public class TransportationCost
    {
        public int Id { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RoundTripCost { get; set; } // تكلفة الرحلة ذهاب وعودة

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Foreign Keys
        public int CityId { get; set; }

        // Navigation Properties
        public City City { get; set; }
    }
}