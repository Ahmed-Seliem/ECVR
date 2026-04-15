using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities;
using ECM.ReservationSystem.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ECM.ReservationSystem.Controllers.Admin
{
    [Route("Admin/[controller]")]
    public class TransportQuotasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransportQuotasController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var viewModel = new TransportQuotaIndexViewModel
            {
                SeasonYear = DateTime.Now.Year,
                IsActive = true,
                Items = await _context.TransportQuotas
                    .Include(q => q.City)
                    .OrderByDescending(q => q.SeasonYear)
                    .ThenBy(q => q.City.NameAr)
                    .ThenBy(q => q.City.Name)
                    .ToListAsync()
            };

            await PopulateCitiesAsync();
            return View(viewModel);
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransportQuotaFormViewModel model)
        {
            await ValidateFormAsync(model);

            if (await _context.TransportQuotas.AnyAsync(q => q.CityId == model.CityId && q.SeasonYear == model.SeasonYear))
            {
                ModelState.AddModelError(string.Empty, "يوجد إعداد نقل مسجل لهذه المدينة وهذا الموسم بالفعل.");
            }

            if (!ModelState.IsValid)
            {
                return await BuildIndexViewAsync(model);
            }

            var entity = new TransportQuota
            {
                CityId = model.CityId,
                SeasonYear = model.SeasonYear,
                BusCount = model.BusCount,
                SeatsPerBus = model.SeatsPerBus,
                IsActive = model.IsActive,
                Notes = model.Notes ?? string.Empty
            };

            _context.TransportQuotas.Add(entity);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حفظ سعة النقل بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TransportQuotaFormViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var entity = await _context.TransportQuotas.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            await ValidateFormAsync(model);

            if (await _context.TransportQuotas.AnyAsync(q => q.Id != id && q.CityId == model.CityId && q.SeasonYear == model.SeasonYear))
            {
                ModelState.AddModelError(string.Empty, "يوجد إعداد نقل آخر لهذه المدينة وهذا الموسم.");
            }

            if (!ModelState.IsValid)
            {
                return await BuildIndexViewAsync(model);
            }

            entity.CityId = model.CityId;
            entity.SeasonYear = model.SeasonYear;
            entity.BusCount = model.BusCount;
            entity.SeatsPerBus = model.SeatsPerBus;
            entity.IsActive = model.IsActive;
            entity.Notes = model.Notes ?? string.Empty;

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تحديث سعة النقل بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.TransportQuotas.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            _context.TransportQuotas.Remove(entity);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف إعداد سعة النقل.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> BuildIndexViewAsync(TransportQuotaFormViewModel form)
        {
            var viewModel = new TransportQuotaIndexViewModel
            {
                Id = form.Id,
                CityId = form.CityId,
                SeasonYear = form.SeasonYear,
                BusCount = form.BusCount,
                SeatsPerBus = form.SeatsPerBus,
                IsActive = form.IsActive,
                Notes = form.Notes,
                Items = await _context.TransportQuotas
                    .Include(q => q.City)
                    .OrderByDescending(q => q.SeasonYear)
                    .ThenBy(q => q.City.NameAr)
                    .ThenBy(q => q.City.Name)
                    .ToListAsync()
            };

            await PopulateCitiesAsync(form.CityId);
            return View("Index", viewModel);
        }

        private async Task PopulateCitiesAsync(int? cityId = null)
        {
            var cities = await _context.Cities
                .Where(c => c.IsActive)
                .OrderBy(c => c.NameAr)
                .ThenBy(c => c.Name)
                .Select(c => new
                {
                    c.Id,
                    DisplayName = string.IsNullOrWhiteSpace(c.NameAr) ? c.Name : c.NameAr
                })
                .ToListAsync();

            ViewBag.Cities = new SelectList(cities, "Id", "DisplayName", cityId);
        }

        private async Task ValidateFormAsync(TransportQuotaFormViewModel model)
        {
            if (model.CityId <= 0)
            {
                ModelState.AddModelError(nameof(model.CityId), "اختر المدينة.");
            }

            if (model.SeasonYear < 2000)
            {
                ModelState.AddModelError(nameof(model.SeasonYear), "أدخل سنة موسم صحيحة.");
            }

            if (model.BusCount <= 0)
            {
                ModelState.AddModelError(nameof(model.BusCount), "عدد الأتوبيسات يجب أن يكون أكبر من صفر.");
            }

            if (model.SeatsPerBus <= 0)
            {
                ModelState.AddModelError(nameof(model.SeatsPerBus), "عدد المقاعد يجب أن يكون أكبر من صفر.");
            }

            if (model.CityId > 0)
            {
                var cityExists = await _context.Cities.AnyAsync(c => c.Id == model.CityId && c.IsActive);
                if (!cityExists)
                {
                    ModelState.AddModelError(nameof(model.CityId), "المدينة المختارة غير موجودة أو غير نشطة.");
                }
            }
        }
    }
}
