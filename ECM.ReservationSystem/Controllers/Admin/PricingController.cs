using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities;
using ECM.ReservationSystem.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ECM.ReservationSystem.Controllers.Admin
{
    [Route("Admin/Pricing")]
    [Route("Admin/Pricings")]
    [Authorize]
    public class PricingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PricingController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(int? unitId)
        {
            var query = _context.Pricings
                .Include(p => p.Unit)
                .ThenInclude(u => u.City)
                .AsQueryable();

            if (unitId.HasValue)
            {
                query = query.Where(p => p.UnitId == unitId.Value);
            }

            var pricings = await query
                .OrderBy(p => p.Unit.City.Name)
                .ThenBy(p => p.Unit.Code)
                .ThenByDescending(p => p.EffectiveFrom)
                .Select(p => new PricingViewModel
                {
                    Id = p.Id,
                    WeeklyRentDefaultCapacity = p.WeeklyRentDefaultCapacity,
                    InsuranceAmount = p.InsuranceAmount,
                    TransportationCostPerPerson = p.TransportationCostPerPerson,
                    EffectiveFrom = p.EffectiveFrom,
                    EffectiveTo = p.EffectiveTo,
                    IsActive = p.IsActive,
                    UnitId = p.UnitId,
                    UnitName = p.Unit.Name,
                    UnitNumber = p.Unit.Code,
                    CityName = p.Unit.City.Name
                })
                .ToListAsync();

            ViewBag.Units = new SelectList(await BuildUnitsSelectListAsync(), "Id", "DisplayName", unitId);
            ViewBag.CurrentFilters = new { unitId };
            return View(pricings);
        }

        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View(new PricingViewModel
            {
                EffectiveFrom = DateTime.Today,
                IsActive = true
            });
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PricingViewModel viewModel)
        {
            await ValidatePricingViewModelAsync(viewModel);

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(viewModel.UnitId);
                return View(viewModel);
            }

            var unit = await _context.Units.FindAsync(viewModel.UnitId);
            if (unit == null)
            {
                ModelState.AddModelError(nameof(PricingViewModel.UnitId), "الوحدة غير موجودة");
                await PopulateDropdowns(viewModel.UnitId);
                return View(viewModel);
            }

            var pricing = new Pricing
            {
                WeeklyRentDefaultCapacity = viewModel.WeeklyRentDefaultCapacity,
                AdditionalPersonCost = 0,
                InsuranceAmount = viewModel.InsuranceAmount,
                TransportationCostPerPerson = viewModel.TransportationCostPerPerson,
                FloorType = unit.FloorType,
                EffectiveFrom = viewModel.EffectiveFrom,
                EffectiveTo = viewModel.EffectiveTo,
                IsActive = viewModel.IsActive,
                UnitId = viewModel.UnitId,
                CreatedAt = DateTime.Now
            };

            _context.Add(pricing);
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم إضافة التسعيرة بنجاح";
            return RedirectToAction(nameof(Index), new { unitId = viewModel.UnitId });
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pricing = await _context.Pricings
                .Include(p => p.Unit)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pricing == null)
            {
                return NotFound();
            }

            var viewModel = new PricingViewModel
            {
                Id = pricing.Id,
                WeeklyRentDefaultCapacity = pricing.WeeklyRentDefaultCapacity,
                InsuranceAmount = pricing.InsuranceAmount,
                TransportationCostPerPerson = pricing.TransportationCostPerPerson,
                EffectiveFrom = pricing.EffectiveFrom,
                EffectiveTo = pricing.EffectiveTo,
                IsActive = pricing.IsActive,
                UnitId = pricing.UnitId
            };

            await PopulateDropdowns(pricing.UnitId);
            return View(viewModel);
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PricingViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            await ValidatePricingViewModelAsync(viewModel, id);

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(viewModel.UnitId);
                return View(viewModel);
            }

            var pricing = await _context.Pricings.FindAsync(id);
            var unit = await _context.Units.FindAsync(viewModel.UnitId);
            if (pricing == null || unit == null)
            {
                return NotFound();
            }

            pricing.WeeklyRentDefaultCapacity = viewModel.WeeklyRentDefaultCapacity;
            pricing.AdditionalPersonCost = 0;
            pricing.InsuranceAmount = viewModel.InsuranceAmount;
            pricing.TransportationCostPerPerson = viewModel.TransportationCostPerPerson;
            pricing.FloorType = unit.FloorType;
            pricing.EffectiveFrom = viewModel.EffectiveFrom;
            pricing.EffectiveTo = viewModel.EffectiveTo;
            pricing.IsActive = viewModel.IsActive;
            pricing.UnitId = viewModel.UnitId;

            _context.Update(pricing);
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم تحديث التسعيرة بنجاح";
            return RedirectToAction(nameof(Index), new { unitId = viewModel.UnitId });
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pricing = await _context.Pricings
                .Include(p => p.Unit)
                .ThenInclude(u => u.City)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pricing == null)
            {
                return NotFound();
            }

            return View(new PricingViewModel
            {
                Id = pricing.Id,
                WeeklyRentDefaultCapacity = pricing.WeeklyRentDefaultCapacity,
                InsuranceAmount = pricing.InsuranceAmount,
                TransportationCostPerPerson = pricing.TransportationCostPerPerson,
                EffectiveFrom = pricing.EffectiveFrom,
                EffectiveTo = pricing.EffectiveTo,
                IsActive = pricing.IsActive,
                UnitId = pricing.UnitId,
                UnitName = pricing.Unit.Name,
                UnitNumber = pricing.Unit.Code,
                CityName = pricing.Unit.City.Name
            });
        }

        [HttpGet("Delete/{id}")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pricing = await _context.Pricings
                .Include(p => p.Unit)
                .ThenInclude(u => u.City)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pricing == null)
            {
                return NotFound();
            }

            return View(new PricingViewModel
            {
                Id = pricing.Id,
                WeeklyRentDefaultCapacity = pricing.WeeklyRentDefaultCapacity,
                InsuranceAmount = pricing.InsuranceAmount,
                TransportationCostPerPerson = pricing.TransportationCostPerPerson,
                EffectiveFrom = pricing.EffectiveFrom,
                EffectiveTo = pricing.EffectiveTo,
                IsActive = pricing.IsActive,
                UnitId = pricing.UnitId,
                UnitName = pricing.Unit.Name,
                UnitNumber = pricing.Unit.Code,
                CityName = pricing.Unit.City.Name
            });
        }

        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var pricing = await _context.Pricings.FindAsync(id);
            if (pricing == null)
            {
                return NotFound();
            }

            var unitId = pricing.UnitId;
            _context.Pricings.Remove(pricing);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف التسعيرة بنجاح";
            return RedirectToAction(nameof(Index), new { unitId });
        }

        private async Task ValidatePricingViewModelAsync(PricingViewModel viewModel, int? pricingId = null)
        {
            var validationContext = new ValidationContext(viewModel);
            var validationResults = new List<ValidationResult>();
            Validator.TryValidateObject(viewModel, validationContext, validationResults, true);

            foreach (var error in validationResults)
            {
                ModelState.AddModelError(error.MemberNames.FirstOrDefault() ?? string.Empty, error.ErrorMessage ?? string.Empty);
            }

            var existingPricing = await _context.Pricings
                .Where(p => p.Id != pricingId &&
                           p.UnitId == viewModel.UnitId &&
                           p.IsActive &&
                           ((p.EffectiveTo == null || p.EffectiveTo > viewModel.EffectiveFrom) &&
                            p.EffectiveFrom < (viewModel.EffectiveTo ?? DateTime.MaxValue)))
                .FirstOrDefaultAsync();

            if (existingPricing != null)
            {
                ModelState.AddModelError(string.Empty, "يوجد تسعيرة أخرى نشطة لنفس الوحدة في نفس الفترة الزمنية");
            }
        }

        private async Task PopulateDropdowns(int? unitId = null)
        {
            ViewBag.Units = new SelectList(await BuildUnitsSelectListAsync(), "Id", "DisplayName", unitId);
        }

        private async Task<List<object>> BuildUnitsSelectListAsync()
        {
            return await _context.Units
                .Include(u => u.City)
                .Include(u => u.UnitType)
                .Where(u => u.IsActive)
                .OrderBy(u => u.City!.Name)
                .ThenBy(u => u.UnitType!.Name)
                .ThenBy(u => u.Code)
                .ThenBy(u => u.Name)
                .Select(u => new
                {
                    u.Id,
                    DisplayName = string.Join(" - ", new[]
                    {
                        string.IsNullOrWhiteSpace(u.City!.NameAr) ? u.City.Name : u.City.NameAr,
                        string.IsNullOrWhiteSpace(u.UnitType!.NameAr) ? u.UnitType.Name : u.UnitType.NameAr,
                        string.IsNullOrWhiteSpace(u.Code) ? u.Name : u.Code
                    })
                })
                .Cast<object>()
                .ToListAsync();
        }

        private bool PricingExists(int id)
        {
            return _context.Pricings.Any(e => e.Id == id);
        }
    }
}
