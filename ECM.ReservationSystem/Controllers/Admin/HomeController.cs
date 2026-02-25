using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Models.ViewModels.Admin;
using ECM.ReservationSystem.Models.Entities;

namespace ECM.ReservationSystem.Controllers.Admin
{
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
            var viewModel = new DashboardViewModel();

            // Basic statistics
            viewModel.TotalCities = await _context.Cities.CountAsync(c => c.IsActive);
            viewModel.TotalUnits = await _context.Units.CountAsync(u => u.IsActive);
            viewModel.TotalReservations = await _context.Reservations.CountAsync();
            viewModel.PendingReservations = await _context.Reservations
                .CountAsync(r => r.Status == ReservationStatus.TemporaryHold);
            viewModel.ConfirmedReservations = await _context.Reservations
                .CountAsync(r => r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Paid);

            // Expired temporary holds
            viewModel.ExpiredTemporaryHolds = await _context.Reservations
                .CountAsync(r => r.Status == ReservationStatus.TemporaryHold
                               && r.PaymentDeadline.HasValue
                               && r.PaymentDeadline < DateTime.Now);

            // Revenue calculations
            viewModel.TotalRevenue = await _context.Reservations
                .Where(r => r.Status == ReservationStatus.Paid || r.Status == ReservationStatus.Approved)
                .SumAsync(r => r.TotalAmount);

            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            viewModel.MonthlyRevenue = await _context.Reservations
                .Where(r => (r.Status == ReservationStatus.Paid || r.Status == ReservationStatus.Approved)
                           && r.CreatedAt.Month == currentMonth
                           && r.CreatedAt.Year == currentYear)
                .SumAsync(r => r.TotalAmount);

            // Recent reservations
            viewModel.RecentReservations = await _context.Reservations
                .Include(r => r.Unit)
                .ThenInclude(u => u.City)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .Select(r => new RecentReservationViewModel
                {
                    Id = r.Id,
                    EmployeeName = r.EmployeeName,
                    UnitName = r.Unit.Name,
                    CityName = r.Unit.City.Name,
                    CheckInDate = r.CheckInDate,
                    CheckOutDate = r.CheckOutDate,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            // Expiring holds (within next 6 hours)
            var expiringDeadline = DateTime.Now.AddHours(6);
            viewModel.ExpiringHolds = await _context.Reservations
                .Include(r => r.Unit)
                .Where(r => r.Status == ReservationStatus.TemporaryHold
                           && r.PaymentDeadline.HasValue
                           && r.PaymentDeadline <= expiringDeadline
                           && r.PaymentDeadline > DateTime.Now)
                .Select(r => new ExpiringHoldViewModel
                {
                    Id = r.Id,
                    EmployeeName = r.EmployeeName,
                    UnitName = r.Unit.Name,
                    PaymentDeadline = r.PaymentDeadline.Value,
                    TimeRemaining = r.PaymentDeadline.Value - DateTime.Now
                })
                .ToListAsync();

            // City statistics
            viewModel.CityStatistics = await _context.Cities
                .Where(c => c.IsActive)
                .Select(c => new CityStatisticsViewModel
                {
                    CityName = c.Name,
                    TotalUnits = c.Units.Count(u => u.IsActive),
                    ReservedUnits = c.Units.Count(u => u.Reservations
                        .Any(r => r.Status == ReservationStatus.Confirmed
                                 || r.Status == ReservationStatus.Paid)),
                    Revenue = c.Units.SelectMany(u => u.Reservations)
                        .Where(r => r.Status == ReservationStatus.Paid
                                   || r.Status == ReservationStatus.Approved)
                        .Sum(r => r.TotalAmount)
                })
                .ToListAsync();

            // Calculate available units for each city
            foreach (var cityStat in viewModel.CityStatistics)
            {
                cityStat.AvailableUnits = cityStat.TotalUnits - cityStat.ReservedUnits;
            }

            return View(viewModel);
        }

        [Route("Units/Availability")]
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

        private List<int> GetAvailableYears()
        {
            var currentYear = DateTime.Now.Year;
            return Enumerable.Range(currentYear, 3).ToList(); // Current year + next 2 years
        }
    }
}