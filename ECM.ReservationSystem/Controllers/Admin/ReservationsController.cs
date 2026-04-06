using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities;

namespace ECM.ReservationSystem.Controllers.Admin
{
    [Route("Admin/[controller]")]
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReservationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Reservations
        [HttpGet("")]
        public async Task<IActionResult> Index(ReservationStatus? status, int? cityId, int? year)
        {
            var query = _context.Reservations
                .Include(r => r.Unit)
                .ThenInclude(u => u.City)
                .Include(r => r.Unit)
                .ThenInclude(u => u.UnitType)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            if (cityId.HasValue)
                query = query.Where(r => r.Unit.CityId == cityId.Value);

            if (year.HasValue)
                query = query.Where(r => r.Unit.Year == year.Value);

            var reservations = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Populate filter dropdowns
            ViewBag.Cities = new SelectList(await _context.Cities.Where(c => c.IsActive).ToListAsync(), "Id", "Name", cityId);
            ViewBag.Statuses = new SelectList(Enum.GetValues(typeof(ReservationStatus)).Cast<ReservationStatus>(), status);
            ViewBag.Years = new SelectList(GetAvailableYears(), year);

            return View(reservations);
        }

        // GET: Admin/Reservations/Details/5
        [HttpGet("Details/{id:int}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.Unit)
                .ThenInclude(u => u.City)
                .Include(r => r.Unit)
                .ThenInclude(u => u.UnitType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reservation == null)
                return NotFound();

            return View(reservation);
        }

        // POST: Admin/Reservations/UpdateStatus/5
        [HttpPost("UpdateStatus/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, ReservationStatus status)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
                return NotFound();

            reservation.Status = status;

            _context.Update(reservation);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تحديث حالة الحجز بنجاح";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost("Cancel/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }

            reservation.Status = ReservationStatus.Cancelled;
            _context.Update(reservation);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إلغاء الحجز بنجاح";
            return RedirectToAction(nameof(Index));
        }

        private List<int> GetAvailableYears()
        {
            var currentYear = DateTime.Now.Year;
            return Enumerable.Range(currentYear - 2, 5).ToList(); // Previous 2 years + current + next 2
        }
    }
}

