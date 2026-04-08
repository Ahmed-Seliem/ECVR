using System.ComponentModel.DataAnnotations;

namespace ECM.ReservationSystem.Models.ViewModels.Admin
{
    public class PricingViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "السعر الأسبوعي مطلوب")]
        [Range(0.01, 999999.99, ErrorMessage = "السعر الأسبوعي يجب أن يكون أكبر من صفر")]
        [Display(Name = "السعر الأسبوعي")]
        public decimal WeeklyRentDefaultCapacity { get; set; }

        [Required(ErrorMessage = "مبلغ التأمين مطلوب")]
        [Range(0, 999999.99, ErrorMessage = "مبلغ التأمين لا يمكن أن يكون سالب")]
        [Display(Name = "مبلغ التأمين")]
        public decimal InsuranceAmount { get; set; }

        [Range(0, 999999.99, ErrorMessage = "سعر النقل للفرد لا يمكن أن يكون سالب")]
        [Display(Name = "سعر النقل للفرد")]
        public decimal TransportationCostPerPerson { get; set; }

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
        public string? UnitNumber { get; set; }

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
