using System.ComponentModel.DataAnnotations;

namespace ECM.ReservationSystem.Models.Entities
{
    public class City
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(100)]
        public string NameAr { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public ICollection<Unit> Units { get; set; } = new List<Unit>();
        public ICollection<TransportationCost> TransportationCosts { get; set; } = new List<TransportationCost>();
    }
}