using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities;
using ECM.ReservationSystem.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECM.ReservationSystem.Controllers.Admin
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Route("")]
        [Route("Dashboard")]
        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;
            var activeDashboardStatuses = new[]
            {
                ReservationStatus.TemporaryHold,
                ReservationStatus.Cancelled,
                ReservationStatus.Approved
            };

            var viewModel = new DashboardViewModel
            {
                TotalCities = await _context.Cities.CountAsync(c => c.IsActive),
                TotalUnits = await _context.Units.CountAsync(u => u.IsActive),
                TotalReservations = await _context.Reservations.CountAsync(r => activeDashboardStatuses.Contains(r.Status)),
                PendingReservations = await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.TemporaryHold),
                ConfirmedReservations = await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.Approved),
                CancelledReservations = await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.Cancelled),
                ExpiredTemporaryHolds = await _context.Reservations.CountAsync(r =>
                    r.Status == ReservationStatus.TemporaryHold &&
                    r.PaymentDeadline.HasValue &&
                    r.PaymentDeadline < now),
                TotalRevenue = await _context.Reservations
                    .Where(r => r.Status == ReservationStatus.Approved)
                    .SumAsync(r => r.TotalAmount),
                MonthlyRevenue = await _context.Reservations
                    .Where(r => r.Status == ReservationStatus.Approved &&
                                r.CreatedAt.Month == now.Month &&
                                r.CreatedAt.Year == now.Year)
                    .SumAsync(r => r.TotalAmount)
            };

            viewModel.RecentReservations = await _context.Reservations
                .Include(r => r.Unit)
                .ThenInclude(u => u.City)
                .Where(r => activeDashboardStatuses.Contains(r.Status))
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .Select(r => new RecentReservationViewModel
                {
                    Id = r.Id,
                    EmployeeName = r.EmployeeName,
                    UnitName = r.Unit.Name,
                    CityName = r.Unit.City.NameAr ?? r.Unit.City.Name,
                    CheckInDate = r.CheckInDate,
                    CheckOutDate = r.CheckOutDate,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            var expiringDeadline = now.AddHours(6);
            viewModel.ExpiringHolds = await _context.Reservations
                .Include(r => r.Unit)
                .Where(r => r.Status == ReservationStatus.TemporaryHold &&
                            r.PaymentDeadline.HasValue &&
                            r.PaymentDeadline <= expiringDeadline &&
                            r.PaymentDeadline > now)
                .Select(r => new ExpiringHoldViewModel
                {
                    Id = r.Id,
                    EmployeeName = r.EmployeeName,
                    UnitName = r.Unit.Name,
                    PaymentDeadline = r.PaymentDeadline.Value,
                    TimeRemaining = r.PaymentDeadline.Value - now
                })
                .ToListAsync();

            viewModel.CityStatistics = await _context.Cities
                .Where(c => c.IsActive)
                .Select(c => new CityStatisticsViewModel
                {
                    CityName = c.NameAr ?? c.Name,
                    TotalUnits = c.Units.Count(u => u.IsActive),
                    ReservedUnits = c.Units.Count(u => u.IsActive && u.Reservations.Any(r => r.Status == ReservationStatus.Approved)),
                    PendingUnits = c.Units.Count(u => u.IsActive && u.Reservations.Any(r => r.Status == ReservationStatus.TemporaryHold)),
                    Revenue = c.Units
                        .SelectMany(u => u.Reservations)
                        .Where(r => r.Status == ReservationStatus.Approved)
                        .Sum(r => r.TotalAmount)
                })
                .ToListAsync();

            foreach (var cityStat in viewModel.CityStatistics)
            {
                cityStat.AvailableUnits = Math.Max(0, cityStat.TotalUnits - cityStat.ReservedUnits - cityStat.PendingUnits);
            }

            return View(viewModel);
        }
    }
}
