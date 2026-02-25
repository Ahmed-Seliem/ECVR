using System.ComponentModel.DataAnnotations;

namespace ECM.ReservationSystem.Models.Entities
{
    public class UnitType
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } // شقة، فيلا، شاليه

        [StringLength(50)]
        public string NameAr { get; set; }

        [StringLength(200)]
        public string Description { get; set; }

        public bool IsForManagement { get; set; } // للإدارة العليا أم للموظفين

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public ICollection<Unit> Units { get; set; } = new List<Unit>();
    }
}