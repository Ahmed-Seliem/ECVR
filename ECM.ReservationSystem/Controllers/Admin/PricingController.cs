using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities;
using ECM.ReservationSystem.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ECM.ReservationSystem.Controllers.Admin
{
    [Route("Admin/[controller]")]
    public class PricingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PricingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Pricing
        [HttpGet("")]
        public async Task<IActionResult> Index(int? unitId, int? floorType)
        {
            var query = _context.Pricings
                .Include(p => p.Unit)
                .ThenInclude(u => u.City)
                .Include(p => p.Unit)
                .ThenInclude(u => u.UnitType)
                .AsQueryable();

            if (unitId.HasValue)
                query = query.Where(p => p.UnitId == unitId.Value);

            if (floorType.HasValue)
                query = query.Where(p => (int)p.FloorType == floorType.Value);

            var pricings = await query
                .OrderBy(p => p.Unit.City.Name)
                .ThenBy(p => p.Unit.Name)
                .ThenBy(p => p.FloorType)
                .Select(p => new PricingViewModel
                {
                    Id = p.Id,
                    WeeklyRentDefaultCapacity = p.WeeklyRentDefaultCapacity,
                    AdditionalPersonCost = p.AdditionalPersonCost,
                    InsuranceAmount = p.InsuranceAmount,
                    TransportationCostPerPerson = p.TransportationCostPerPerson,
                    FloorType = p.FloorType,
                    EffectiveFrom = p.EffectiveFrom,
                    EffectiveTo = p.EffectiveTo,
                    IsActive = p.IsActive,
                    UnitId = p.UnitId,
                    UnitName = p.Unit.Name,
                    CityName = p.Unit.City.Name,
                    UnitTypeName = p.Unit.UnitType.Name,
                    FloorTypeDisplay = GetFloorTypeDisplayName(p.FloorType)
                })
                .ToListAsync();

            ViewBag.Units = new SelectList(
                await _context.Units
                    .Include(u => u.City)
                    .Include(u => u.UnitType)
                    .Where(u => u.IsActive)
                    .Select(u => new { u.Id, DisplayName = $"{u.City.Name} - {u.UnitType.Name} - {u.Name}" })
                    .ToListAsync(),
                "Id", "DisplayName", unitId);

            ViewBag.FloorTypes = new SelectList(
                Enum.GetValues(typeof(FloorType)).Cast<FloorType>()
                    .Select(f => new { Value = (int)f, Text = GetFloorTypeDisplayName(f) }),
                "Value", "Text", floorType);

            ViewBag.CurrentFilters = new { unitId, floorType };

            return View(pricings);
        }

        // GET: Admin/Pricing/Create
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            var viewModel = new PricingViewModel
            {
                EffectiveFrom = DateTime.Today,
                IsActive = true
            };
            return View(viewModel);
        }

        // POST: Admin/Pricing/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PricingViewModel viewModel)
        {

            var validationContext = new ValidationContext(viewModel);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(viewModel, validationContext, validationResults, true);

            if (!isValid)
            {
                foreach (var error in validationResults)
                {
                    ModelState.AddModelError(error.MemberNames.FirstOrDefault() ?? "", error.ErrorMessage);
                }
            }

            if (ModelState.IsValid)
            {
                var existingPricing = await _context.Pricings
                    .Where(p => p.UnitId == viewModel.UnitId &&
                               p.FloorType == viewModel.FloorType &&
                               p.IsActive &&
                               ((p.EffectiveTo == null || p.EffectiveTo > viewModel.EffectiveFrom) &&
                                p.EffectiveFrom < (viewModel.EffectiveTo ?? DateTime.MaxValue)))
                    .FirstOrDefaultAsync();

                if (existingPricing != null)
                {
                    ModelState.AddModelError("", "يوجد تسعيرة أخرى نشطة لنفس الوحدة ونوع الدور في نفس الفترة الزمنية");
                }
            }

            if (ModelState.IsValid)
            {
                var pricing = new Pricing
                {
                    WeeklyRentDefaultCapacity = viewModel.WeeklyRentDefaultCapacity,
                    AdditionalPersonCost = viewModel.AdditionalPersonCost,
                    InsuranceAmount = viewModel.InsuranceAmount,
                    TransportationCostPerPerson = viewModel.TransportationCostPerPerson,
                    FloorType = viewModel.FloorType,
                    EffectiveFrom = viewModel.EffectiveFrom,
                    EffectiveTo = viewModel.EffectiveTo,
                    IsActive = viewModel.IsActive,
                    UnitId = viewModel.UnitId,
                    CreatedAt = DateTime.Now
                };

                _context.Add(pricing);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم إضافة التسعيرة بنجاح";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns();
            return View(viewModel);
        }

        // GET: Admin/Pricing/Edit/5
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var pricing = await _context.Pricings.FindAsync(id);
            if (pricing == null)
                return NotFound();

            var viewModel = new PricingViewModel
            {
                Id = pricing.Id,
                WeeklyRentDefaultCapacity = pricing.WeeklyRentDefaultCapacity,
                AdditionalPersonCost = pricing.AdditionalPersonCost,
                InsuranceAmount = pricing.InsuranceAmount,
                TransportationCostPerPerson = pricing.TransportationCostPerPerson,
                FloorType = pricing.FloorType,
                EffectiveFrom = pricing.EffectiveFrom,
                EffectiveTo = pricing.EffectiveTo,
                IsActive = pricing.IsActive,
                UnitId = pricing.UnitId
            };

            await PopulateDropdowns();
            return View(viewModel);
        }

        // POST: Admin/Pricing/Edit/5
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PricingViewModel viewModel)
        {
            if (id != viewModel.Id)
                return NotFound();

            // التحقق من صحة التواريخ
            if (viewModel.EffectiveTo.HasValue && viewModel.EffectiveTo <= viewModel.EffectiveFrom)
            {
                ModelState.AddModelError("EffectiveTo", "تاريخ انتهاء السريان يجب أن يكون بعد تاريخ بداية السريان");
            }

            // التحقق من عدم وجود تسعيرة أخرى نشطة لنفس الوحدة ونوع الدور في نفس الفترة (باستثناء التسعيرة الحالية)
            var existingPricing = await _context.Pricings
                .Where(p => p.Id != viewModel.Id &&
                           p.UnitId == viewModel.UnitId &&
                           p.FloorType == viewModel.FloorType &&
                           p.IsActive &&
                           ((p.EffectiveTo == null || p.EffectiveTo > viewModel.EffectiveFrom) &&
                            p.EffectiveFrom < (viewModel.EffectiveTo ?? DateTime.MaxValue)))
                .FirstOrDefaultAsync();

            if (existingPricing != null)
            {
                ModelState.AddModelError("", "يوجد تسعيرة أخرى نشطة لنفس الوحدة ونوع الدور في نفس الفترة الزمنية");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var pricing = await _context.Pricings.FindAsync(id);
                    if (pricing == null)
                        return NotFound();

                    pricing.WeeklyRentDefaultCapacity = viewModel.WeeklyRentDefaultCapacity;
                    pricing.AdditionalPersonCost = viewModel.AdditionalPersonCost;
                    pricing.InsuranceAmount = viewModel.InsuranceAmount;
                    pricing.TransportationCostPerPerson = viewModel.TransportationCostPerPerson;
                    pricing.FloorType = viewModel.FloorType;
                    pricing.EffectiveFrom = viewModel.EffectiveFrom;
                    pricing.EffectiveTo = viewModel.EffectiveTo;
                    pricing.IsActive = viewModel.IsActive;
                    pricing.UnitId = viewModel.UnitId;

                    _context.Update(pricing);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "تم تحديث التسعيرة بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PricingExists(viewModel.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns();
            return View(viewModel);
        }

        // GET: Admin/Pricing/Details/5
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var pricing = await _context.Pricings
                .Include(p => p.Unit)
                .ThenInclude(u => u.City)
                .Include(p => p.Unit)
                .ThenInclude(u => u.UnitType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pricing == null)
                return NotFound();

            var viewModel = new PricingViewModel
            {
                Id = pricing.Id,
                WeeklyRentDefaultCapacity = pricing.WeeklyRentDefaultCapacity,
                AdditionalPersonCost = pricing.AdditionalPersonCost,
                InsuranceAmount = pricing.InsuranceAmount,
                TransportationCostPerPerson = pricing.TransportationCostPerPerson,
                FloorType = pricing.FloorType,
                EffectiveFrom = pricing.EffectiveFrom,
                EffectiveTo = pricing.EffectiveTo,
                IsActive = pricing.IsActive,
                UnitId = pricing.UnitId,
                UnitName = pricing.Unit.Name,
                CityName = pricing.Unit.City.Name,
                UnitTypeName = pricing.Unit.UnitType.Name,
                FloorTypeDisplay = GetFloorTypeDisplayName(pricing.FloorType)
            };

            return View(viewModel);
        }

        // GET: Admin/Pricing/Delete/5
        [HttpGet("Delete/{id}")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var pricing = await _context.Pricings
                .Include(p => p.Unit)
                .ThenInclude(u => u.City)
                .Include(p => p.Unit)
                .ThenInclude(u => u.UnitType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pricing == null)
                return NotFound();

            var viewModel = new PricingViewModel
            {
                Id = pricing.Id,
                WeeklyRentDefaultCapacity = pricing.WeeklyRentDefaultCapacity,
                AdditionalPersonCost = pricing.AdditionalPersonCost,
                InsuranceAmount = pricing.InsuranceAmount,
                TransportationCostPerPerson = pricing.TransportationCostPerPerson,
                FloorType = pricing.FloorType,
                EffectiveFrom = pricing.EffectiveFrom,
                EffectiveTo = pricing.EffectiveTo,
                IsActive = pricing.IsActive,
                UnitId = pricing.UnitId,
                UnitName = pricing.Unit.Name,
                CityName = pricing.Unit.City.Name,
                UnitTypeName = pricing.Unit.UnitType.Name,
                FloorTypeDisplay = GetFloorTypeDisplayName(pricing.FloorType)
            };

            return View(viewModel);
        }

        // POST: Admin/Pricing/Delete/5
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var pricing = await _context.Pricings.FindAsync(id);
            if (pricing == null)
                return NotFound();

            _context.Pricings.Remove(pricing);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف التسعيرة بنجاح";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns()
        {
            ViewBag.Units = new SelectList(
                await _context.Units
                    .Include(u => u.City)
                    .Include(u => u.UnitType)
                    .Where(u => u.IsActive)
                    .Select(u => new { u.Id, DisplayName = $"{u.City.Name} - {u.UnitType.Name} - {u.Name}" })
                    .ToListAsync(),
                "Id", "DisplayName");

            ViewBag.FloorTypes = new SelectList(
                Enum.GetValues(typeof(FloorType)).Cast<FloorType>()
                    .Select(f => new { Value = (int)f, Text = GetFloorTypeDisplayName(f) }),
                "Value", "Text");
        }

        private static string GetFloorTypeDisplayName(FloorType floorType)
        {
            return floorType switch
            {
                FloorType.GroundFloor => "دور أرضي",
                FloorType.MiddleFloor => "دور متكرر",
                FloorType.TopFloor => "دور أخير",
                _ => floorType.ToString()
            };
        }

        private bool PricingExists(int id)
        {
            return _context.Pricings.Any(e => e.Id == id);
        }
    }
}
