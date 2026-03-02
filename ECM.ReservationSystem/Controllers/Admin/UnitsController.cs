using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Models.Entities;

namespace ECM.ReservationSystem.Controllers.Admin
{
    [Route("Admin/[controller]")]
    public class UnitsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UnitsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Units
        [HttpGet("")]
        public async Task<IActionResult> Index(int? cityId, int? unitTypeId, int? year)
        {
            var query = _context.Units
                .Include(u => u.City)
                .Include(u => u.UnitType)
                .AsQueryable();

            if (cityId.HasValue)
                query = query.Where(u => u.CityId == cityId.Value);

            if (unitTypeId.HasValue)
                query = query.Where(u => u.UnitTypeId == unitTypeId.Value);

            if (year.HasValue)
                query = query.Where(u => u.Year == year.Value);

            var units = await query.OrderBy(u => u.City.Name)
                .ThenBy(u => u.Name)
                .ToListAsync();

            ViewBag.Cities = new SelectList(await _context.Cities.Where(c => c.IsActive).ToListAsync(), "Id", "Name", cityId);
            ViewBag.UnitTypes = new SelectList(await _context.UnitTypes.Where(ut => ut.IsActive).ToListAsync(), "Id", "Name", unitTypeId);
            ViewBag.Years = new SelectList(GetAvailableYears(), year);
            ViewBag.FloorTypes = new SelectList(new[]
            {
                new { Value = (int)FloorType.GroundFloor, Text = "دور أرضي" },
                new { Value = (int)FloorType.MiddleFloor, Text = "دور متكرر" },
                new { Value = (int)FloorType.TopFloor, Text = "دور أخير" }
            }, "Value", "Text");

            ViewBag.CurrentFilters = new { cityId, unitTypeId, year };

            return View(units);
        }

        // GET: Admin/Units/Create
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();

            // للتشخيص - تحقق من البيانات
            var cities = await _context.Cities.Where(c => c.IsActive).CountAsync();
            var unitTypes = await _context.UnitTypes.Where(ut => ut.IsActive).CountAsync();

            System.Diagnostics.Debug.WriteLine($"Cities Count: {cities}");
            System.Diagnostics.Debug.WriteLine($"UnitTypes Count: {unitTypes}");

            return View();
        }

        // POST: Admin/Units/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Unit unit)
        {
            // للتشخيص - طباعة البيانات المستلمة
            System.Diagnostics.Debug.WriteLine($"=== Unit Create Debug ===");
            System.Diagnostics.Debug.WriteLine($"Name: {unit.Name}");
            System.Diagnostics.Debug.WriteLine($"Code: {unit.Code}");
            System.Diagnostics.Debug.WriteLine($"CityId: {unit.CityId}");
            System.Diagnostics.Debug.WriteLine($"UnitTypeId: {unit.UnitTypeId}");
            System.Diagnostics.Debug.WriteLine($"FloorType: {unit.FloorType}");
            System.Diagnostics.Debug.WriteLine($"FloorNumber: {unit.FloorNumber}");
            System.Diagnostics.Debug.WriteLine($"DefaultCapacity: {unit.DefaultCapacity}");
            System.Diagnostics.Debug.WriteLine($"MaxCapacity: {unit.MaxCapacity}");
            System.Diagnostics.Debug.WriteLine($"Year: {unit.Year}");
            System.Diagnostics.Debug.WriteLine($"IsActive: {unit.IsActive}");

            // طباعة أخطاء الـ ModelState
            if (!ModelState.IsValid)
            {
                System.Diagnostics.Debug.WriteLine("=== ModelState Errors ===");
                foreach (var modelError in ModelState)
                {
                    var key = modelError.Key;
                    var errors = modelError.Value.Errors;
                    foreach (var error in errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Field: {key}, Error: {error.ErrorMessage}");
                    }
                }
            }

            // تحقق إضافي من صحة البيانات
            if (unit.MaxCapacity > 0 && unit.DefaultCapacity > 0 && unit.MaxCapacity < unit.DefaultCapacity)
            {
                ModelState.AddModelError("MaxCapacity", "السعة القصوى يجب أن تكون أكبر من أو تساوي السعة الافتراضية");
            }

            // تحقق من أن الـ Code فريد (فقط إذا كان موجود)
            if (!string.IsNullOrWhiteSpace(unit.Code) && await _context.Units.AnyAsync(u => u.Code == unit.Code))
            {
                ModelState.AddModelError("Code", "كود الوحدة موجود مسبقاً، يرجى اختيار كود آخر");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    unit.CreatedAt = DateTime.Now;
                    _context.Add(unit);
                    await _context.SaveChangesAsync();

                    System.Diagnostics.Debug.WriteLine($"Unit saved successfully with ID: {unit.Id}");

                    TempData["Success"] = "تم إضافة الوحدة بنجاح";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving unit: {ex.Message}");
                    ModelState.AddModelError("", $"حدث خطأ أثناء الحفظ: {ex.Message}");
                }
            }

            await PopulateDropdowns();
            return View(unit);
        }

        // GET: Admin/Units/Edit/5
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var unit = await _context.Units.FindAsync(id);
            if (unit == null)
                return NotFound();

            await PopulateDropdowns();
            return View(unit);
        }

        // POST: Admin/Units/Edit/5
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Code,Description,DefaultCapacity,MaxCapacity,FloorType,FloorNumber,Year,IsActive,CityId,UnitTypeId,CreatedAt")] Unit unit)
        {
            if (id != unit.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(unit);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "تم تحديث الوحدة بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UnitExists(unit.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            await PopulateDropdowns();
            return View(unit);
        }

        // GET: Admin/Units/Details/5
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var unit = await _context.Units
                .Include(u => u.City)
                .Include(u => u.UnitType)
                .Include(u => u.Pricings)
                .Include(u => u.Reservations)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (unit == null)
                return NotFound();

            return View(unit);
        }

        // GET: Admin/Units/Delete/5
        [HttpGet("Delete/{id}")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var unit = await _context.Units
                .Include(u => u.City)
                .Include(u => u.UnitType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (unit == null)
                return NotFound();

            return View(unit);
        }

        // POST: Admin/Units/Delete/5
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var unit = await _context.Units.FindAsync(id);
            if (unit == null)
                return NotFound();

            _context.Units.Remove(unit);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف الوحدة بنجاح";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns()
        {
            var cities = await _context.Cities.Where(c => c.IsActive).ToListAsync();
            var unitTypes = await _context.UnitTypes.Where(ut => ut.IsActive).ToListAsync();

            ViewBag.Cities = new SelectList(cities, "Id", "Name");
            ViewBag.UnitTypes = new SelectList(unitTypes, "Id", "Name");

            // إصلاح FloorTypes - تأكد من القيم الصحيحة
            var floorTypes = new[]
            {
                new { Value = (int)FloorType.GroundFloor, Text = "دور أرضي" },
                new { Value = (int)FloorType.MiddleFloor, Text = "دور متكرر" },
                new { Value = (int)FloorType.TopFloor, Text = "دور أخير" }
            };

            ViewBag.FloorTypes = new SelectList(floorTypes, "Value", "Text");
            ViewBag.Years = new SelectList(GetAvailableYears());

            // للتشخيص
            System.Diagnostics.Debug.WriteLine($"Cities loaded: {cities.Count}");
            System.Diagnostics.Debug.WriteLine($"UnitTypes loaded: {unitTypes.Count}");
        }

        private List<int> GetAvailableYears()
        {
            var currentYear = DateTime.Now.Year;
            return Enumerable.Range(currentYear, 5).ToList(); // Current year + next 4 years
        }

        private string GetFloorTypeDisplayName(FloorType floorType)
        {
            return floorType switch
            {
                FloorType.GroundFloor => "دور أرضي",
                FloorType.MiddleFloor => "دور متكرر",
                FloorType.TopFloor => "دور أخير",
                _ => floorType.ToString()
            };
        }

        private bool UnitExists(int id)
        {
            return _context.Units.Any(e => e.Id == id);
        }
    }
}
