using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities.OneDayTrips;
using ECM.ReservationSystem.Models.ViewModels.OneDayTrips;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ECM.ReservationSystem.Controllers.Admin;

[Route("Admin/[controller]")]
[Authorize]
public class OneDayTripReportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public OneDayTripReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? locationId, TripBookingType? bookingType, BookingStatus? status, int? year, int? month)
    {
        var model = await BuildModelAsync(locationId, bookingType, status, year, month);
        await PopulateFiltersAsync(locationId, bookingType, status, year, month);
        return View(model);
    }

    [HttpGet("ExportExcel")]
    public async Task<IActionResult> ExportExcel(int? locationId, TripBookingType? bookingType, BookingStatus? status, int? year, int? month)
    {
        var model = await BuildModelAsync(locationId, bookingType, status, year, month);
        var content = BuildExcelHtml(model);
        var fileName = $"one-day-trip-reports-{DateTime.Now:yyyyMMdd-HHmmss}.xls";
        return File(Encoding.UTF8.GetBytes(content), "application/vnd.ms-excel; charset=utf-8", fileName);
    }

    [HttpGet("ExportPdf")]
    public async Task<IActionResult> ExportPdf(int? locationId, TripBookingType? bookingType, BookingStatus? status, int? year, int? month)
    {
        var model = await BuildModelAsync(locationId, bookingType, status, year, month);
        return View("Print", model);
    }

    private async Task<OneDayTripReportsViewModel> BuildModelAsync(int? locationId, TripBookingType? bookingType, BookingStatus? status, int? year, int? month)
    {
        var query = _context.TripBookings
            .Include(b => b.Trip)
                .ThenInclude(t => t.TripLocation)
            .AsNoTracking()
            .AsQueryable();

        if (locationId.HasValue)
        {
            query = query.Where(b => b.Trip.TripLocationId == locationId.Value);
        }

        if (bookingType.HasValue)
        {
            query = query.Where(b => b.BookingType == bookingType.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }

        if (year.HasValue)
        {
            query = query.Where(b => b.Trip.TripDate.Year == year.Value);
        }

        if (month.HasValue)
        {
            query = query.Where(b => b.Trip.TripDate.Month == month.Value);
        }

        var bookings = await query
            .OrderBy(b => b.Trip.TripLocation.NameAr ?? b.Trip.TripLocation.Name)
            .ThenBy(b => b.Trip.TripDate)
            .ThenBy(b => b.EmployeeName)
            .ToListAsync();

        var groups = bookings
            .GroupBy(b => new
            {
                LocationName = string.IsNullOrWhiteSpace(b.Trip.TripLocation.NameAr)
                    ? b.Trip.TripLocation.Name
                    : b.Trip.TripLocation.NameAr,
                b.Trip.TripDate
            })
            .Select(group => new OneDayTripReportGroupViewModel
            {
                LocationName = group.Key.LocationName,
                TripDate = group.Key.TripDate,
                BookingCount = group.Count(),
                PersonCount = group.Sum(b => b.AdultsCount + b.ChildrenCount + b.CompanionsCount),
                TotalAmount = group.Sum(b => b.TotalAmount),
                Items = group.Select(b => new OneDayTripReportItemViewModel
                {
                    BookingId = b.Id,
                    EmployeeName = b.EmployeeName,
                    EmployeeNumber = b.EmployeeNumber,
                    Sector = b.Sector,
                    PhoneNumber = b.PhoneNumber,
                    AdultsCount = b.AdultsCount,
                    ChildrenCount = b.ChildrenCount,
                    CompanionsCount = b.CompanionsCount,
                    TotalPersons = b.AdultsCount + b.ChildrenCount + b.CompanionsCount,
                    TotalAmount = b.TotalAmount,
                    BookingTypeText = GetBookingTypeText(b.BookingType),
                    StatusText = GetStatusText(b.Status)
                }).ToList()
            })
            .OrderBy(g => g.TripDate)
            .ThenBy(g => g.LocationName)
            .ToList();

        return new OneDayTripReportsViewModel
        {
            LocationId = locationId,
            BookingType = bookingType,
            Status = status,
            Year = year,
            Month = month,
            TotalBookings = bookings.Count,
            TotalPersons = bookings.Sum(b => b.AdultsCount + b.ChildrenCount + b.CompanionsCount),
            GrandTotal = bookings.Sum(b => b.TotalAmount),
            Groups = groups
        };
    }

    private async Task PopulateFiltersAsync(int? locationId, TripBookingType? bookingType, BookingStatus? status, int? year, int? month)
    {
        ViewBag.Years = Enumerable.Range(DateTime.Now.Year - 2, 5).ToList();
        ViewBag.SelectedYear = year;
        ViewBag.SelectedMonth = month;

        ViewBag.Locations = new SelectList(
            await _context.TripLocations.OrderBy(l => l.Name).ToListAsync(),
            "Id",
            "Name",
            locationId);

        ViewBag.BookingTypes = new SelectList(
            new[]
            {
                new { Value = TripBookingType.Employee, Text = "موظفين" },
                new { Value = TripBookingType.Pension, Text = "معاشات" }
            },
            "Value",
            "Text",
            bookingType);

        ViewBag.Statuses = new SelectList(
            new[]
            {
                new { Value = BookingStatus.PendingPayment, Text = "بانتظار الدفع" },
                new { Value = BookingStatus.Confirmed, Text = "مؤكد" },
                new { Value = BookingStatus.Cancelled, Text = "ملغي" }
            },
            "Value",
            "Text",
            status);
    }

    private static string GetBookingTypeText(TripBookingType type) => type switch
    {
        TripBookingType.Employee => "موظفين",
        TripBookingType.Pension => "معاشات",
        _ => type.ToString()
    };

    private static string GetStatusText(BookingStatus status) => status switch
    {
        BookingStatus.PendingPayment => "بانتظار الدفع",
        BookingStatus.Confirmed => "مؤكد",
        BookingStatus.Cancelled => "ملغي",
        _ => status.ToString()
    };

    private static string BuildExcelHtml(OneDayTripReportsViewModel model)
    {
        var html = new StringBuilder();
        html.AppendLine("<html><head><meta charset='utf-8' /></head><body dir='rtl'>");
        html.AppendLine("<table border='1' style='border-collapse:collapse;width:100%;font-family:Tahoma;'>");
        html.AppendLine("<thead>");
        html.AppendLine("<tr style='background:#f3ead8;font-weight:bold;'>");
        html.AppendLine("<th>المكان</th><th>تاريخ الرحلة</th><th>الاسم</th><th>الرقم</th><th>القطاع</th><th>التليفون</th><th>بالغين</th><th>أطفال</th><th>مرافقين</th><th>الإجمالي</th><th>نوع الحجز</th><th>الحالة</th>");
        html.AppendLine("</tr>");
        html.AppendLine("</thead><tbody>");

        foreach (var group in model.Groups)
        {
            foreach (var item in group.Items)
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td>{group.LocationName}</td>");
                html.AppendLine($"<td>{group.TripDate:dd/MM/yyyy}</td>");
                html.AppendLine($"<td>{item.EmployeeName}</td>");
                html.AppendLine($"<td>{item.EmployeeNumber}</td>");
                html.AppendLine($"<td>{(string.IsNullOrWhiteSpace(item.Sector) ? "-" : item.Sector)}</td>");
                html.AppendLine($"<td>{(string.IsNullOrWhiteSpace(item.PhoneNumber) ? "-" : item.PhoneNumber)}</td>");
                html.AppendLine($"<td>{item.AdultsCount}</td>");
                html.AppendLine($"<td>{item.ChildrenCount}</td>");
                html.AppendLine($"<td>{item.CompanionsCount}</td>");
                html.AppendLine($"<td>{item.TotalAmount:N2}</td>");
                html.AppendLine($"<td>{item.BookingTypeText}</td>");
                html.AppendLine($"<td>{item.StatusText}</td>");
                html.AppendLine("</tr>");
            }
        }

        html.AppendLine("</tbody></table></body></html>");
        return html.ToString();
    }
}
