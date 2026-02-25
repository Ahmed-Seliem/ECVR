using System.ComponentModel.DataAnnotations;
using ECM.ReservationSystem.Models.Entities;

namespace ECM.ReservationSystem.Models.ViewModels.Admin
{
    public class UnitViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الوحدة مطلوب")]
        [Display(Name = "اسم الوحدة")]
        public string Name { get; set; }

        [Display(Name = "رمز الوحدة")]
        public string Code { get; set; }

        [Display(Name = "الوصف")]
        public string Description { get; set; }

        [Required(ErrorMessage = "السعة الافتراضية مطلوبة")]
        [Range(1, 20, ErrorMessage = "السعة الافتراضية يجب أن تكون بين 1 و 20")]
        [Display(Name = "السعة الافتراضية (6 أشخاص)")]
        public int DefaultCapacity { get; set; } = 6;

        [Required(ErrorMessage = "الحد الأقصى للسعة مطلوب")]
        [Range(1, 30, ErrorMessage = "الحد الأقصى للسعة يجب أن يكون بين 1 و 30")]
        [Display(Name = "الحد الأقصى للسعة")]
        public int MaxCapacity { get; set; }

        [Required(ErrorMessage = "نوع الدور مطلوب")]
        [Display(Name = "نوع الدور")]
        public FloorType FloorType { get; set; }

        [Required(ErrorMessage = "رقم الدور مطلوب")]
        [Range(0, 50, ErrorMessage = "رقم الدور يجب أن يكون بين 0 و 50")]
        [Display(Name = "رقم الدور")]
        public int FloorNumber { get; set; }

        [Required(ErrorMessage = "السنة مطلوبة")]
        [Range(2024, 2030, ErrorMessage = "السنة يجب أن تكون بين 2024 و 2030")]
        [Display(Name = "السنة")]
        public int Year { get; set; }

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "المدينة مطلوبة")]
        [Display(Name = "المدينة")]
        public int CityId { get; set; }

        [Required(ErrorMessage = "نوع الوحدة مطلوب")]
        [Display(Name = "نوع الوحدة")]
        public int UnitTypeId { get; set; }

        // For display purposes
        public string CityName { get; set; }
        public string UnitTypeName { get; set; }
        public string FloorTypeDisplay { get; set; }
    }
}