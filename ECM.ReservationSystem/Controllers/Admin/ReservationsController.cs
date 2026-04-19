using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities;
using ECM.ReservationSystem.Models.ViewModels.Admin;
using ECM.ReservationSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ECM.ReservationSystem.Controllers.Admin
{
    [Route("Admin/[controller]")]
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IReservationService _reservationService;

        public ReservationsController(ApplicationDbContext context, IReservationService reservationService)
        {
            _context = context;
            _reservationService = reservationService;
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

        [HttpGet("WeeklyReport")]
        public async Task<IActionResult> WeeklyReport(int? cityId, int? year)
        {
            var targetYear = year ?? DateTime.Now.Year;

            var query = _context.Reservations
                .Include(r => r.Unit)
                .ThenInclude(u => u.City)
                .Where(r => r.Status != ReservationStatus.Cancelled && r.CheckInDate.Year == targetYear);

            if (cityId.HasValue)
            {
                query = query.Where(r => r.Unit.CityId == cityId.Value);
            }

            var reservations = await query
                .OrderBy(r => r.Unit.City.Name)
                .ThenBy(r => r.CheckInDate)
                .ThenBy(r => r.EmployeeName)
                .ToListAsync();

            var report = reservations
                .GroupBy(r => new
                {
                    CityName = r.Unit.City.NameAr ?? r.Unit.City.Name,
                    r.CheckInDate,
                    WeekEndDate = r.CheckOutDate
                })
                .Select(group => new WeeklyReservationReportGroupViewModel
                {
                    CityName = group.Key.CityName,
                    WeekStartDate = group.Key.CheckInDate,
                    WeekEndDate = group.Key.WeekEndDate,
                    ReservationCount = group.Count(),
                    PassengerCount = group.Sum(r => r.NumberOfGuests),
                    Reservations = group.Select(r => new WeeklyReservationReportItemViewModel
                    {
                        ReservationId = r.Id,
                        EmployeeName = r.EmployeeName,
                        EmployeeNumber = r.EmployeeNumber,
                        PhoneNumber = r.PhoneNumber,
                        UnitName = r.Unit.Name,
                        NumberOfGuests = r.NumberOfGuests,
                        Status = r.Status.ToString()
                    }).ToList()
                })
                .OrderBy(g => g.WeekStartDate)
                .ThenBy(g => g.CityName)
                .ToList();

            ViewBag.Cities = new SelectList(await _context.Cities.Where(c => c.IsActive).ToListAsync(), "Id", "Name", cityId);
            ViewBag.Years = new SelectList(GetAvailableYears(), year);

            return View(report);
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

            if (status == ReservationStatus.Paid)
            {
                await _reservationService.ConfirmPaymentAsync(id);
            }
            else if (status == ReservationStatus.Cancelled)
            {
                await _reservationService.CancelReservationAsync(id);
            }
            else
            {
                reservation.Status = status;

                _context.Update(reservation);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "تم تحديث حالة الحجز بنجاح";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost("Cancel/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var reservationExists = await _context.Reservations.AnyAsync(r => r.Id == id);
            if (!reservationExists)
            {
                return NotFound();
            }

            await _reservationService.CancelReservationAsync(id);

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

