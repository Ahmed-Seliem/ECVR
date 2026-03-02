using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("Admin/[controller]")]
public class CitiesController : Controller
{
    private readonly ApplicationDbContext _context;

    public CitiesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Admin/Cities
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var cities = await _context.Cities
            .Include(c => c.Units)
            .OrderBy(c => c.Name)
            .ToListAsync();
        return View(cities);
    }

    // GET: Admin/Cities/Details/5
    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var city = await _context.Cities
            .Include(c => c.Units)
            .Include(c => c.TransportationCosts)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (city == null)
            return NotFound();

        return View(city);
    }

    // GET: Admin/Cities/Create
    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View();
    }

    // POST: Admin/Cities/Create
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,NameAr,IsActive")] City city)
    {
        if (ModelState.IsValid)
        {
            city.CreatedAt = DateTime.Now;
            _context.Add(city);
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم إضافة المدينة بنجاح";
            return RedirectToAction(nameof(Index));
        }
        return View(city);
    }

    // GET: Admin/Cities/Edit/5
    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var city = await _context.Cities.FindAsync(id);
        if (city == null)
            return NotFound();

        return View(city);
    }

    // POST: Admin/Cities/Edit/5
    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,NameAr,IsActive,CreatedAt")] City city)
    {
        if (id != city.Id)
            return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(city);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم تحديث المدينة بنجاح";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CityExists(city.Id))
                    return NotFound();
                else
                    throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(city);
    }

    // GET: Admin/Cities/Delete/5
    [HttpGet("Delete/{id}")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var city = await _context.Cities
            .FirstOrDefaultAsync(m => m.Id == id);
        if (city == null)
            return NotFound();

        return View(city);
    }

    // POST: Admin/Cities/Delete/5
    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var city = await _context.Cities.FindAsync(id);
        if (city != null)
        {
            var hasUnits = await _context.Units.AnyAsync(u => u.CityId == id);
            if (hasUnits)
            {
                TempData["Error"] = "لا يمكن حذف المدينة لأنها تحتوي على وحدات";
                return RedirectToAction(nameof(Index));
            }

            _context.Cities.Remove(city);
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم حذف المدينة بنجاح";
        }

        return RedirectToAction(nameof(Index));
    }

    private bool CityExists(int id)
    {
        return _context.Cities.Any(e => e.Id == id);
    }
}
