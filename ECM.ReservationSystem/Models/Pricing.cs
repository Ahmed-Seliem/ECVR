using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECM.ReservationSystem.Models.Entities
{
    public class Pricing
    {
        public int Id { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal WeeklyRentDefaultCapacity { get; set; } // سعر الإيجار الأسبوعي للسعة الافتراضية (6 أشخاص)

        [Column(TypeName = "decimal(18,2)")]
        public decimal AdditionalPersonCost { get; set; } // تكلفة الشخص الإضافي

        [Column(TypeName = "decimal(18,2)")]
        public decimal InsuranceAmount { get; set; }

        public FloorType FloorType { get; set; } // نوع الدور (لأن كل دور له سعر مختلف)

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Foreign Keys
        public int UnitId { get; set; }

        // Navigation Properties
        public Unit Unit { get; set; }

        // Helper Method لحساب التكلفة الإجمالية
        public decimal CalculateWeeklyRent(int numberOfGuests)
        {
            if (numberOfGuests <= 6)
                return WeeklyRentDefaultCapacity;

            var additionalGuests = numberOfGuests - 6;
            return WeeklyRentDefaultCapacity + (additionalGuests * AdditionalPersonCost);
        }
    }
}