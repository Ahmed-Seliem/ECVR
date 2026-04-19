using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("Admin/[controller]")]
[Authorize]
public class UnitTypesController : Controller
{
    private readonly ApplicationDbContext _context;

    public UnitTypesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Admin/UnitTypes
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var unitTypes = await _context.UnitTypes
            .OrderBy(ut => ut.Name)
            .ToListAsync();
        return View(unitTypes);
    }

    // GET: Admin/UnitTypes/Create
    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View();
    }

    // POST: Admin/UnitTypes/Create
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,NameAr,Description,IsForManagement,IsActive")] UnitType unitType)
    {
        NormalizeUnitType(unitType);
        if (ModelState.IsValid)
        {
            _context.Add(unitType);
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم إضافة نوع الوحدة بنجاح";
            return RedirectToAction(nameof(Index));
        }
        return View(unitType);
    }

    // GET: Admin/UnitTypes/Edit/5
    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var unitType = await _context.UnitTypes.FindAsync(id);
        if (unitType == null)
            return NotFound();

        return View(unitType);
    }

    // POST: Admin/UnitTypes/Edit/5
    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,NameAr,Description,IsForManagement,IsActive")] UnitType unitType)
    {
        if (id != unitType.Id)
            return NotFound();

        NormalizeUnitType(unitType);
        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(unitType);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم تحديث نوع الوحدة بنجاح";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UnitTypeExists(unitType.Id))
                    return NotFound();
                else
                    throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(unitType);
    }

    // GET: Admin/UnitTypes/Delete/5
    [HttpGet("Delete/{id}")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var unitType = await _context.UnitTypes.FirstOrDefaultAsync(m => m.Id == id);
        if (unitType == null)
            return NotFound();

        return View(unitType);
    }

    // POST: Admin/UnitTypes/Delete/5
    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var unitType = await _context.UnitTypes.FindAsync(id);
        if (unitType != null)
        {
            var hasUnits = await _context.Units.AnyAsync(u => u.UnitTypeId == id);
            if (hasUnits)
            {
                TempData["Error"] = "لا يمكن حذف نوع الوحدة لأنه مرتبط بوحدات";
                return RedirectToAction(nameof(Index));
            }

            _context.UnitTypes.Remove(unitType);
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم حذف نوع الوحدة بنجاح";
        }
        return RedirectToAction(nameof(Index));
    }

    private bool UnitTypeExists(int id)
    {
        return _context.UnitTypes.Any(e => e.Id == id);
    }

    private static void NormalizeUnitType(UnitType unitType)
    {
        unitType.Name = unitType.Name?.Trim() ?? string.Empty;
        unitType.NameAr = unitType.NameAr?.Trim() ?? string.Empty;
        unitType.Description = string.IsNullOrWhiteSpace(unitType.Description)
            ? string.Empty
            : unitType.Description.Trim();
    }
}

