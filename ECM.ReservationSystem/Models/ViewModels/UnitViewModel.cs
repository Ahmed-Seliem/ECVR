using System.ComponentModel.DataAnnotations;
using ECM.ReservationSystem.Domain.Entities;

namespace ECM.ReservationSystem.Models.ViewModels.Admin
{
    public class UnitViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الوحدة مطلوب")]
        [Display(Name = "اسم الوحدة")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "رقم الوحدة")]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "الوصف")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "السعة مطلوبة")]
        [Range(1, 20, ErrorMessage = "السعة يجب أن تكون بين 1 و 20")]
        [Display(Name = "السعة")]
        public int DefaultCapacity { get; set; } = 6;

        [Required(ErrorMessage = "عدد الغرف مطلوب")]
        [Range(1, 20, ErrorMessage = "عدد الغرف يجب أن يكون بين 1 و 20")]
        [Display(Name = "عدد الغرف")]
        public int RoomCount { get; set; } = 1;

        [Required(ErrorMessage = "نوع الدور مطلوب")]
        [Display(Name = "نوع الدور")]
        public FloorType FloorType { get; set; }

        [Required(ErrorMessage = "رقم الدور مطلوب")]
        [Range(0, 50, ErrorMessage = "رقم الدور يجب أن يكون بين 0 و 50")]
        [Display(Name = "رقم الدور")]
        public int FloorNumber { get; set; }

        [Required(ErrorMessage = "السنة مطلوبة")]
        [Range(2024, 2035, ErrorMessage = "السنة يجب أن تكون بين 2024 و 2035")]
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

        [Display(Name = "الواجهة")]
        public int? UnitFacadeId { get; set; }

        public bool IsForPensioners { get; set; }
        public string CityName { get; set; } = string.Empty;
        public string UnitTypeName { get; set; } = string.Empty;
        public string FacadeName { get; set; } = string.Empty;
        public string FloorTypeDisplay { get; set; } = string.Empty;
    }
}
