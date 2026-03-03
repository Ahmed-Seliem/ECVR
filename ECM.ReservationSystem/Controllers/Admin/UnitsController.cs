using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities;
using ECM.ReservationSystem.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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

            // Debug: verify dropdown data exists
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
            // Debug: print incoming values
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

            // Debug: print model state errors
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

            // Business rule: max capacity must be >= default capacity
            if (unit.MaxCapacity > 0 && unit.DefaultCapacity > 0 && unit.MaxCapacity < unit.DefaultCapacity)
            {
                ModelState.AddModelError("MaxCapacity", "السعة القصوى يجب أن تكون أكبر من أو تساوي السعة الافتراضية");
            }

            // Business rule: Code must be unique when provided
            if (!string.IsNullOrWhiteSpace(unit.Code) && await _context.Units.AnyAsync(u => u.Code == unit.Code))
            {
                ModelState.AddModelError("Code", "كود الوحدة موجود مسبقاً، يرجى اختيار كود آخر");
            }

            if (ModelState.IsValid)
            {
                try
                {
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Code,Description,DefaultCapacity,MaxCapacity,FloorType,FloorNumber,Year,IsActive,CityId,UnitTypeId")] Unit unit)
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

        [HttpGet("Availability")]
        public async Task<IActionResult> UnitsAvailability(int? cityId, int? year)
        {
            var currentYear = year ?? DateTime.Now.Year;

            var query = _context.Units
                .Include(u => u.City)
                .Include(u => u.UnitType)
                .Include(u => u.Reservations)
                .Where(u => u.IsActive && u.Year == currentYear);

            if (cityId.HasValue)
                query = query.Where(u => u.CityId == cityId.Value);

            var units = await query.ToListAsync();

            var viewModels = units.Select(unit => new UnitAvailabilityViewModel
            {
                UnitId = unit.Id,
                UnitName = unit.Name,
                CityName = unit.City.Name,
                UnitTypeName = unit.UnitType.Name,
                FloorType = unit.FloorType,
                Year = unit.Year,
                WeekAvailabilities = GetWeekAvailabilities(unit, currentYear)
            }).ToList();

            ViewBag.Cities = await _context.Cities.Where(c => c.IsActive).ToListAsync();
            ViewBag.CurrentCityId = cityId;
            ViewBag.CurrentYear = currentYear;
            ViewBag.AvailableYears = GetAvailableYears();

            return View(viewModels);
        }

        private List<WeekAvailabilityViewModel> GetWeekAvailabilities(Unit unit, int year)
        {
            var weekAvailabilities = new List<WeekAvailabilityViewModel>();
            var startDate = new DateTime(year, 1, 1);
            var endDate = new DateTime(year, 12, 31);

            // Find first Friday of the year
            while (startDate.DayOfWeek != DayOfWeek.Friday)
            {
                startDate = startDate.AddDays(1);
            }

            var currentDate = startDate;
            while (currentDate <= endDate)
            {
                var weekEnd = currentDate.AddDays(6); // Friday to Thursday (7 days)

                var reservation = unit.Reservations
                    .FirstOrDefault(r => r.CheckInDate <= currentDate && r.CheckOutDate >= weekEnd
                                        && (r.Status == ReservationStatus.Confirmed
                                           || r.Status == ReservationStatus.Paid
                                           || r.Status == ReservationStatus.TemporaryHold));

                weekAvailabilities.Add(new WeekAvailabilityViewModel
                {
                    WeekStartDate = currentDate,
                    WeekEndDate = weekEnd,
                    IsAvailable = reservation == null,
                    IsReserved = reservation != null,
                    ReservationStatus = reservation?.Status.ToString(),
                    EmployeeName = reservation?.EmployeeName
                });

                currentDate = currentDate.AddDays(7); // Move to next Friday
            }

            return weekAvailabilities;
        }

        private async Task PopulateDropdowns()
        {
            var cities = await _context.Cities.Where(c => c.IsActive).ToListAsync();
            var unitTypes = await _context.UnitTypes.Where(ut => ut.IsActive).ToListAsync();

            ViewBag.Cities = new SelectList(cities, "Id", "Name");
            ViewBag.UnitTypes = new SelectList(unitTypes, "Id", "Name");

            // Floor types
            var floorTypes = new[]
            {
                new { Value = (int)FloorType.GroundFloor, Text = "دور أرضي" },
                new { Value = (int)FloorType.MiddleFloor, Text = "دور متكرر" },
                new { Value = (int)FloorType.TopFloor, Text = "دور أخير" }
            };

            ViewBag.FloorTypes = new SelectList(floorTypes, "Value", "Text");
            ViewBag.Years = new SelectList(GetAvailableYears());

            // Debug
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

