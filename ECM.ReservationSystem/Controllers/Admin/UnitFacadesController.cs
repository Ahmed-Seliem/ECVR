using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("Admin/[controller]")]
public class UnitFacadesController : Controller
{
    private readonly ApplicationDbContext _context;

    public UnitFacadesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var unitFacades = await _context.UnitFacades
            .OrderBy(f => f.NameAr)
            .ThenBy(f => f.Name)
            .ToListAsync();

        return View(unitFacades);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,NameAr,IsActive")] UnitFacade unitFacade)
    {
        await ValidateFacadeAsync(unitFacade);

        if (!ModelState.IsValid)
        {
            return View(unitFacade);
        }

        _context.Add(unitFacade);
        await _context.SaveChangesAsync();
        TempData["Success"] = "تمت إضافة الواجهة بنجاح";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var unitFacade = await _context.UnitFacades.FindAsync(id);
        if (unitFacade == null)
        {
            return NotFound();
        }

        return View(unitFacade);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,NameAr,IsActive")] UnitFacade unitFacade)
    {
        if (id != unitFacade.Id)
        {
            return NotFound();
        }

        await ValidateFacadeAsync(unitFacade, id);

        if (!ModelState.IsValid)
        {
            return View(unitFacade);
        }

        try
        {
            _context.Update(unitFacade);
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم تحديث الواجهة بنجاح";
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!UnitFacadeExists(unitFacade.Id))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Delete/{id}")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var unitFacade = await _context.UnitFacades.FirstOrDefaultAsync(m => m.Id == id);
        if (unitFacade == null)
        {
            return NotFound();
        }

        return View(unitFacade);
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var unitFacade = await _context.UnitFacades.FindAsync(id);
        if (unitFacade != null)
        {
            var hasUnits = await _context.Units.AnyAsync(u => u.UnitFacadeId == id);
            if (hasUnits)
            {
                TempData["Error"] = "لا يمكن حذف الواجهة لأنها مرتبطة بوحدات";
                return RedirectToAction(nameof(Index));
            }

            _context.UnitFacades.Remove(unitFacade);
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم حذف الواجهة بنجاح";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateFacadeAsync(UnitFacade unitFacade, int? currentId = null)
    {
        unitFacade.Name = unitFacade.Name?.Trim() ?? string.Empty;
        unitFacade.NameAr = unitFacade.NameAr?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(unitFacade.Name))
        {
            ModelState.AddModelError(nameof(UnitFacade.Name), "الاسم الإنجليزي مطلوب");
        }

        if (string.IsNullOrWhiteSpace(unitFacade.NameAr))
        {
            ModelState.AddModelError(nameof(UnitFacade.NameAr), "الاسم العربي مطلوب");
        }

        if (!string.IsNullOrWhiteSpace(unitFacade.Name))
        {
            var exists = await _context.UnitFacades.AnyAsync(f =>
                f.Name == unitFacade.Name &&
                (!currentId.HasValue || f.Id != currentId.Value));

            if (exists)
            {
                ModelState.AddModelError(nameof(UnitFacade.Name), "الاسم الإنجليزي مستخدم بالفعل");
            }
        }
    }

    private bool UnitFacadeExists(int id)
    {
        return _context.UnitFacades.Any(e => e.Id == id);
    }
}
