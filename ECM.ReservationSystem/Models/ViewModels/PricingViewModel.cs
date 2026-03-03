using System.ComponentModel.DataAnnotations;
using ECM.ReservationSystem.Domain.Entities;

namespace ECM.ReservationSystem.Models.ViewModels.Admin
{
    public class PricingViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "سعر الإيجار الأسبوعي للسعة الافتراضية مطلوب")]
        [Range(0.01, 999999.99, ErrorMessage = "سعر الإيجار يجب أن يكون أكبر من صفر")]
        [Display(Name = "سعر الإيجار الأسبوعي (6 أشخاص)")]
        public decimal WeeklyRentDefaultCapacity { get; set; }

        [Required(ErrorMessage = "تكلفة الشخص الإضافي مطلوبة")]
        [Range(0, 999999.99, ErrorMessage = "تكلفة الشخص الإضافي لا يمكن أن تكون سالبة")]
        [Display(Name = "تكلفة الشخص الإضافي")]
        public decimal AdditionalPersonCost { get; set; }

        [Required(ErrorMessage = "مبلغ التأمين مطلوب")]
        [Range(0, 999999.99, ErrorMessage = "مبلغ التأمين لا يمكن أن يكون سالب")]
        [Display(Name = "مبلغ التأمين")]
        public decimal InsuranceAmount { get; set; }

        [Required(ErrorMessage = "نوع الدور مطلوب")]
        [Display(Name = "نوع الدور")]
        public FloorType FloorType { get; set; }

        [Required(ErrorMessage = "تاريخ بداية السريان مطلوب")]
        [Display(Name = "تاريخ بداية السريان")]
        [DataType(DataType.Date)]
        public DateTime EffectiveFrom { get; set; }

        [Display(Name = "تاريخ انتهاء السريان")]
        [DataType(DataType.Date)]
        public DateTime? EffectiveTo { get; set; }

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "الوحدة مطلوبة")]
        [Display(Name = "الوحدة")]
        public int UnitId { get; set; }

  
        public string? UnitName { get; set; }
        public string? CityName { get; set; }
        public string? UnitTypeName { get; set; }
        public string? FloorTypeDisplay { get; set; }

        // Validation method
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EffectiveTo.HasValue && EffectiveTo <= EffectiveFrom)
            {
                yield return new ValidationResult(
                    "تاريخ انتهاء السريان يجب أن يكون بعد تاريخ بداية السريان",
                    new[] { nameof(EffectiveTo) });
            }
        }
    }
}
