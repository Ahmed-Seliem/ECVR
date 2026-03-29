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

        [HttpGet("")]
        public async Task<IActionResult> Index(int? cityId, int? unitTypeId, int? year)
        {
            var query = _context.Units
                .Include(u => u.City)
                .Include(u => u.UnitType)
                .AsQueryable();

            if (cityId.HasValue)
            {
                query = query.Where(u => u.CityId == cityId.Value);
            }

            if (unitTypeId.HasValue)
            {
                query = query.Where(u => u.UnitTypeId == unitTypeId.Value);
            }

            if (year.HasValue)
            {
                query = query.Where(u => u.Year == year.Value);
            }

            var units = await query
                .OrderBy(u => u.City!.Name)
                .ThenBy(u => u.Name)
                .ToListAsync();

            ViewBag.Cities = new SelectList(await _context.Cities.Where(c => c.IsActive).ToListAsync(), "Id", "Name", cityId);
            ViewBag.UnitTypes = new SelectList(await _context.UnitTypes.Where(ut => ut.IsActive).ToListAsync(), "Id", "Name", unitTypeId);
            ViewBag.Years = new SelectList(GetAvailableYears(), year);
            ViewBag.FloorTypes = new SelectList(GetFloorTypeOptions(), "Value", "Text");

            return View(units);
        }

        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View();
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Unit unit)
        {
            await ValidateUnitAsync(unit);

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns();
                return View(unit);
            }

            _context.Add(unit);
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم إضافة الوحدة بنجاح";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var unit = await _context.Units
                .Include(u => u.City)
                .Include(u => u.UnitType)
                .FirstOrDefaultAsync(u => u.Id == id.Value);

            if (unit == null)
            {
                return NotFound();
            }

            await PopulateDropdowns(unit.CityId, unit.UnitTypeId, unit.Year);
            ViewData["CurrentCity"] = unit.City?.Name;
            ViewData["CurrentUnitType"] = unit.UnitType?.Name;
            return View(unit);
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Code,Description,DefaultCapacity,MaxCapacity,FloorType,FloorNumber,Year,IsActive,IsForPensioners,CityId,UnitTypeId,CreatedAt,CreatedByUserId")] Unit unit)
        {
            if (id != unit.Id)
            {
                return NotFound();
            }

            await ValidateUnitAsync(unit, id);

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(unit.CityId, unit.UnitTypeId, unit.Year);
                return View(unit);
            }

            try
            {
                _context.Update(unit);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم تحديث الوحدة بنجاح";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UnitExists(unit.Id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var unit = await _context.Units
                .Include(u => u.City)
                .Include(u => u.UnitType)
                .Include(u => u.Pricings)
                .Include(u => u.Reservations)
                .Include(u => u.ScheduleSlots)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (unit == null)
            {
                return NotFound();
            }

            return View(unit);
        }

        [HttpGet("Delete/{id}")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var unit = await _context.Units
                .Include(u => u.City)
                .Include(u => u.UnitType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (unit == null)
            {
                return NotFound();
            }

            return View(unit);
        }

        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var unit = await _context.Units.FindAsync(id);
            if (unit == null)
            {
                return NotFound();
            }

            _context.Units.Remove(unit);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف الوحدة بنجاح";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Availability")]
        public async Task<IActionResult> UnitsAvailability(int? cityId, int? unitTypeId, int? unitId, int? year, bool? isForManagement)
        {
            var currentYear = year ?? DateTime.Now.Year;

            var query = _context.Units
                .Include(u => u.City)
                .Include(u => u.UnitType)
                .Include(u => u.Reservations)
                .Include(u => u.ScheduleSlots)
                .Where(u => u.IsActive && u.Year == currentYear);

            if (cityId.HasValue)
            {
                query = query.Where(u => u.CityId == cityId.Value);
            }

            if (unitTypeId.HasValue)
            {
                query = query.Where(u => u.UnitTypeId == unitTypeId.Value);
            }

            if (unitId.HasValue)
            {
                query = query.Where(u => u.Id == unitId.Value);
            }

            if (isForManagement.HasValue)
            {
                query = query.Where(u => u.UnitType != null && u.UnitType.IsForManagement == isForManagement.Value);
            }

            var units = await query
                .OrderBy(u => u.City!.Name)
                .ThenBy(u => u.UnitType!.Name)
                .ThenBy(u => u.Name)
                .ToListAsync();

            var viewModel = new UnitsAvailabilityPageViewModel
            {
                Year = currentYear,
                CityId = cityId,
                UnitTypeId = unitTypeId,
                UnitId = unitId,
                IsForManagement = isForManagement,
                Units = units.Select(BuildAvailabilityViewModel).ToList()
            };

            ViewBag.Cities = new SelectList(await _context.Cities.Where(c => c.IsActive).ToListAsync(), "Id", "Name", cityId);
            ViewBag.UnitTypes = new SelectList(await _context.UnitTypes.Where(ut => ut.IsActive).ToListAsync(), "Id", "Name", unitTypeId);
            ViewBag.Units = new SelectList(await GetUnitsForAvailabilityFilterAsync(cityId, unitTypeId, currentYear, isForManagement), "Id", "DisplayName", unitId);
            ViewBag.AvailableYears = GetAvailableYears();
            ViewBag.AudienceOptions = new SelectList(new[]
            {
                new { Value = "", Text = "الكل" },
                new { Value = "false", Text = "وحدات الموظفين" },
                new { Value = "true", Text = "وحدات الإدارة" }
            }, "Value", "Text", isForManagement?.ToString().ToLowerInvariant());

            return View(viewModel);
        }

        [HttpPost("Availability/CreateSchedule")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSchedule(CreateUnitScheduleRequest request)
        {
            if (request.UnitId <= 0)
            {
                TempData["Error"] = "اختر الوحدة أولاً.";
                return RedirectToAction(nameof(UnitsAvailability), new { year = request.Year });
            }

            if (request.StartMonth < 1 || request.StartMonth > 12 || request.EndMonth < 1 || request.EndMonth > 12 || request.EndMonth < request.StartMonth)
            {
                TempData["Error"] = "فترة الجدولة غير صحيحة.";
                return RedirectToAction(nameof(UnitsAvailability), new { unitId = request.UnitId, year = request.Year });
            }

            var unit = await _context.Units.FindAsync(request.UnitId);
            if (unit == null)
            {
                TempData["Error"] = "الوحدة غير موجودة.";
                return RedirectToAction(nameof(UnitsAvailability), new { year = request.Year });
            }

            var startDate = GetFirstFridayOnOrAfter(new DateTime(request.Year, request.StartMonth, 1));
            var lastDay = new DateTime(request.Year, request.EndMonth, DateTime.DaysInMonth(request.Year, request.EndMonth));

            var newSlots = new List<UnitScheduleSlot>();
            for (var slotStart = startDate; slotStart <= lastDay; slotStart = slotStart.AddDays(7))
            {
                var slotEnd = slotStart.AddDays(6);
                if (slotEnd > lastDay)
                {
                    break;
                }

                var exists = await _context.UnitScheduleSlots.AnyAsync(s =>
                    s.UnitId == request.UnitId &&
                    s.SlotStartDate == slotStart &&
                    s.SlotEndDate == slotEnd);

                if (!exists)
                {
                    newSlots.Add(new UnitScheduleSlot
                    {
                        UnitId = request.UnitId,
                        Year = request.Year,
                        Name = BuildSlotName(slotStart, slotEnd, request.SlotName),
                        SlotStartDate = slotStart,
                        SlotEndDate = slotEnd,
                        IsActive = true,
                        Notes = request.Notes
                    });
                }
                else
                {
                    var existingSlot = await _context.UnitScheduleSlots.FirstAsync(s =>
                        s.UnitId == request.UnitId &&
                        s.SlotStartDate == slotStart &&
                        s.SlotEndDate == slotEnd);

                    if (!existingSlot.IsActive)
                    {
                        existingSlot.IsActive = true;
                        existingSlot.Name = BuildSlotName(slotStart, slotEnd, request.SlotName);
                        existingSlot.Notes = request.Notes;
                    }
                }
            }

            if (newSlots.Any())
            {
                _context.UnitScheduleSlots.AddRange(newSlots);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"تم إنشاء {newSlots.Count} أسبوع للوحدة.";
            }
            else
            {
                TempData["Info"] = "لا توجد أسابيع جديدة لإضافتها في الفترة المختارة.";
            }

            return RedirectToAction(nameof(UnitsAvailability), new
            {
                unitId = request.UnitId,
                year = request.Year,
                cityId = request.CityId,
                unitTypeId = request.UnitTypeId,
                isForManagement = request.IsForManagement
            });
        }

        [HttpPost("Availability/DeleteSlot/{slotId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSlot(int slotId, int? cityId, int? unitTypeId, int? unitId, int? year, bool? isForManagement)
        {
            var slot = await _context.UnitScheduleSlots
                .Include(s => s.Unit)
                .ThenInclude(u => u!.Reservations)
                .FirstOrDefaultAsync(s => s.Id == slotId);

            if (slot == null)
            {
                TempData["Error"] = "الـ slot غير موجود.";
                return RedirectToAction(nameof(UnitsAvailability), new { cityId, unitTypeId, unitId, year, isForManagement });
            }

            var hasReservation = slot.Unit.Reservations.Any(r =>
                r.Status != ReservationStatus.Cancelled &&
                r.CheckInDate < slot.SlotEndDate.AddDays(1) &&
                r.CheckOutDate > slot.SlotStartDate);

            if (hasReservation)
            {
                TempData["Error"] = "لا يمكن حذف slot عليه حجز.";
                return RedirectToAction(nameof(UnitsAvailability), new { cityId, unitTypeId, unitId, year, isForManagement });
            }

            slot.IsActive = false;
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم إلغاء الـ slot بنجاح.";

            return RedirectToAction(nameof(UnitsAvailability), new { cityId, unitTypeId, unitId, year, isForManagement });
        }

        private UnitAvailabilityViewModel BuildAvailabilityViewModel(Unit unit)
        {
            var weekAvailabilities = unit.ScheduleSlots
                .Where(s => s.IsActive)
                .OrderBy(s => s.SlotStartDate)
                .Select(slot =>
                {
                    var reservation = unit.Reservations
                        .Where(r => r.Status != ReservationStatus.Cancelled
                                    && r.CheckInDate < slot.SlotEndDate.AddDays(1)
                                    && r.CheckOutDate > slot.SlotStartDate)
                        .OrderByDescending(r => r.CreatedAt)
                        .FirstOrDefault();

                    return new WeekAvailabilityViewModel
                    {
                        SlotId = slot.Id,
                        SlotName = slot.Name,
                        WeekStartDate = slot.SlotStartDate,
                        WeekEndDate = slot.SlotEndDate,
                        IsScheduled = true,
                        IsAvailable = reservation == null,
                        IsReserved = reservation != null,
                        ReservationStatus = reservation?.Status.ToString() ?? string.Empty,
                        EmployeeName = reservation?.EmployeeName ?? string.Empty,
                        Notes = slot.Notes ?? string.Empty
                    };
                })
                .ToList();

            return new UnitAvailabilityViewModel
            {
                UnitId = unit.Id,
                UnitName = unit.Name,
                CityName = unit.City?.Name ?? string.Empty,
                UnitTypeName = unit.UnitType?.Name ?? string.Empty,
                FloorType = unit.FloorType,
                Year = unit.Year,
                IsForPensioners = unit.IsForPensioners,
                IsForManagement = unit.UnitType?.IsForManagement ?? false,
                WeekAvailabilities = weekAvailabilities
            };
        }

        private async Task ValidateUnitAsync(Unit unit, int? currentUnitId = null)
        {
            if (unit.MaxCapacity > 0 && unit.DefaultCapacity > 0 && unit.MaxCapacity < unit.DefaultCapacity)
            {
                ModelState.AddModelError(nameof(Unit.MaxCapacity), "السعة القصوى يجب أن تكون أكبر من أو تساوي السعة الافتراضية");
            }

            if (!string.IsNullOrWhiteSpace(unit.Code))
            {
                var codeExists = await _context.Units.AnyAsync(u => u.Code == unit.Code && (!currentUnitId.HasValue || u.Id != currentUnitId.Value));
                if (codeExists)
                {
                    ModelState.AddModelError(nameof(Unit.Code), "كود الوحدة موجود مسبقاً");
                }
            }
        }

        private async Task PopulateDropdowns(int? cityId = null, int? unitTypeId = null, int? year = null)
        {
            ViewBag.Cities = new SelectList(await _context.Cities.Where(c => c.IsActive).ToListAsync(), "Id", "Name", cityId);
            ViewBag.UnitTypes = new SelectList(await _context.UnitTypes.Where(ut => ut.IsActive).ToListAsync(), "Id", "Name", unitTypeId);
            ViewBag.FloorTypes = new SelectList(GetFloorTypeOptions(), "Value", "Text");
            ViewBag.Years = new SelectList(GetAvailableYears(), year);
        }

        private async Task<List<object>> GetUnitsForAvailabilityFilterAsync(int? cityId, int? unitTypeId, int year, bool? isForManagement)
        {
            var query = _context.Units
                .Include(u => u.City)
                .Include(u => u.UnitType)
                .Where(u => u.IsActive && u.Year == year)
                .AsQueryable();

            if (cityId.HasValue)
            {
                query = query.Where(u => u.CityId == cityId.Value);
            }

            if (unitTypeId.HasValue)
            {
                query = query.Where(u => u.UnitTypeId == unitTypeId.Value);
            }

            if (isForManagement.HasValue)
            {
                query = query.Where(u => u.UnitType != null && u.UnitType.IsForManagement == isForManagement.Value);
            }

            return await query
                .OrderBy(u => u.City!.Name)
                .ThenBy(u => u.Name)
                .Select(u => new
                {
                    u.Id,
                    DisplayName = $"{u.City!.Name} - {u.UnitType!.Name} - {u.Name}"
                })
                .Cast<object>()
                .ToListAsync();
        }

        private static List<object> GetFloorTypeOptions()
        {
            return new List<object>
            {
                new { Value = (int)FloorType.GroundFloor, Text = "دور أرضي" },
                new { Value = (int)FloorType.MiddleFloor, Text = "دور متكرر" },
                new { Value = (int)FloorType.TopFloor, Text = "دور أخير" }
            };
        }

        private List<int> GetAvailableYears()
        {
            var currentYear = DateTime.Now.Year;
            return Enumerable.Range(currentYear, 5).ToList();
        }

        private static DateTime GetFirstFridayOnOrAfter(DateTime date)
        {
            while (date.DayOfWeek != DayOfWeek.Friday)
            {
                date = date.AddDays(1);
            }

            return date;
        }

        private static string BuildSlotName(DateTime startDate, DateTime endDate, string? customName)
        {
            var defaultName = $"{startDate:dd/MM/yyyy} إلى {endDate:dd/MM/yyyy}";
            return string.IsNullOrWhiteSpace(customName)
                ? defaultName
                : $"{customName.Trim()} | {defaultName}";
        }

        private bool UnitExists(int id)
        {
            return _context.Units.Any(e => e.Id == id);
        }

        public class CreateUnitScheduleRequest
        {
            public int UnitId { get; set; }
            public int Year { get; set; }
            public int StartMonth { get; set; }
            public int EndMonth { get; set; }
            public string? SlotName { get; set; }
            public string? Notes { get; set; }
            public int? CityId { get; set; }
            public int? UnitTypeId { get; set; }
            public bool? IsForManagement { get; set; }
        }
    }
}
