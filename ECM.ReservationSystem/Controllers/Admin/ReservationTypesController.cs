using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("Admin/[controller]")]
[Authorize]
public class ReservationTypesController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReservationTypesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var types = await _context.ReservationTypes
            .OrderBy(x => x.Name)
            .ToListAsync();
        return View(types);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,NameAr,Description,MaxGuestsLimit,IsActive")] ReservationType model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "بيانات غير صحيحة.";
            return RedirectToAction(nameof(Index));
        }

        if (model.MaxGuestsLimit < 1)
        {
            model.MaxGuestsLimit = 1;
        }

        _context.ReservationTypes.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "تم إضافة نوع الحجز بنجاح";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,NameAr,Description,MaxGuestsLimit,IsActive")] ReservationType model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "بيانات غير صحيحة.";
            return RedirectToAction(nameof(Index));
        }

        var existing = await _context.ReservationTypes.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.Name = model.Name;
        existing.NameAr = model.NameAr;
        existing.Description = model.Description;
        existing.MaxGuestsLimit = model.MaxGuestsLimit < 1 ? 1 : model.MaxGuestsLimit;
        existing.IsActive = model.IsActive;

        await _context.SaveChangesAsync();
        TempData["Success"] = "تم تحديث نوع الحجز بنجاح";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var model = await _context.ReservationTypes.FindAsync(id);
        if (model == null)
        {
            return NotFound();
        }

        _context.ReservationTypes.Remove(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "تم حذف نوع الحجز بنجاح";
        return RedirectToAction(nameof(Index));
    }
}
