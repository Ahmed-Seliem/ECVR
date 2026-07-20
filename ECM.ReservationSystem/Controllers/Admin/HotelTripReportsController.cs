using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities.HotelTrips;
using ECM.ReservationSystem.Models.ViewModels.HotelTrips;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ECM.ReservationSystem.Controllers.Admin;

[Route("Admin/[controller]")]
[Authorize]
public class HotelTripReportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public HotelTripReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? hotelId, HotelBookingType? bookingType, HotelBookingStatus? status, int? year, int? month)
    {
        var model = await BuildModelAsync(hotelId, bookingType, status, year, month);
        await PopulateFiltersAsync(hotelId, bookingType, status, year, month);
        return View(model);
    }

    [HttpGet("ExportExcel")]
    public async Task<IActionResult> ExportExcel(int? hotelId, HotelBookingType? bookingType, HotelBookingStatus? status, int? year, int? month)
    {
        var model = await BuildModelAsync(hotelId, bookingType, status, year, month);
        var content = BuildExcelHtml(model);
        var fileName = $"hotel-trip-reports-{DateTime.Now:yyyyMMdd-HHmmss}.xls";
        return File(Encoding.UTF8.GetBytes(content), "application/vnd.ms-excel; charset=utf-8", fileName);
    }

    [HttpGet("ExportPdf")]
    public async Task<IActionResult> ExportPdf(int? hotelId, HotelBookingType? bookingType, HotelBookingStatus? status, int? year, int? month)
    {
        var model = await BuildModelAsync(hotelId, bookingType, status, year, month);
        return View("Print", model);
    }

    private async Task<HotelTripReportsViewModel> BuildModelAsync(int? hotelId, HotelBookingType? bookingType, HotelBookingStatus? status, int? year, int? month)
    {
        var query = _context.HotelTripBookings
            .Include(b => b.HotelTrip)
                .ThenInclude(t => t.Hotel)
                    .ThenInclude(h => h.HotelCity)
            .AsNoTracking()
            .AsQueryable();

        if (hotelId.HasValue)
        {
            query = query.Where(b => b.HotelTrip.HotelId == hotelId.Value);
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
            query = query.Where(b => b.HotelTrip.StartDate.Year == year.Value);
        }

        if (month.HasValue)
        {
            query = query.Where(b => b.HotelTrip.StartDate.Month == month.Value);
        }

        var bookings = await query
            .OrderBy(b => b.HotelTrip.Hotel.NameAr ?? b.HotelTrip.Hotel.Name)
            .ThenBy(b => b.HotelTrip.StartDate)
            .ThenBy(b => b.EmployeeName)
            .ToListAsync();

        var groups = bookings
            .GroupBy(b => new
            {
                HotelName = string.IsNullOrWhiteSpace(b.HotelTrip.Hotel.NameAr)
                    ? b.HotelTrip.Hotel.Name
                    : b.HotelTrip.Hotel.NameAr,
                CityName = b.HotelTrip.Hotel.HotelCity == null
                    ? string.Empty
                    : (string.IsNullOrWhiteSpace(b.HotelTrip.Hotel.HotelCity.NameAr)
                        ? b.HotelTrip.Hotel.HotelCity.Name
                        : b.HotelTrip.Hotel.HotelCity.NameAr),
                b.HotelTrip.StartDate,
                b.HotelTrip.EndDate
            })
            .Select(group => new HotelTripReportGroupViewModel
            {
                HotelName = group.Key.HotelName,
                CityName = group.Key.CityName,
                StartDate = group.Key.StartDate,
                EndDate = group.Key.EndDate,
                BookingCount = group.Count(),
                PersonCount = group.Sum(b => b.AdultsCount + b.ChildrenCount + b.CompanionsCount),
                TotalAmount = group.Sum(b => b.TotalAmount),
                Items = group.Select(b => new HotelTripReportItemViewModel
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
            .OrderBy(g => g.StartDate)
            .ThenBy(g => g.HotelName)
            .ToList();

        return new HotelTripReportsViewModel
        {
            HotelId = hotelId,
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

    private async Task PopulateFiltersAsync(int? hotelId, HotelBookingType? bookingType, HotelBookingStatus? status, int? year, int? month)
    {
        ViewBag.Years = Enumerable.Range(DateTime.Now.Year - 2, 5).ToList();
        ViewBag.SelectedYear = year;
        ViewBag.SelectedMonth = month;

        ViewBag.Hotels = new SelectList(
            await _context.Hotels.OrderBy(h => h.Name).ToListAsync(),
            "Id",
            "Name",
            hotelId);

        ViewBag.BookingTypes = new SelectList(
            new[]
            {
                new { Value = HotelBookingType.Employee, Text = "موظفين" },
                new { Value = HotelBookingType.Pension, Text = "معاشات" }
            },
            "Value",
            "Text",
            bookingType);

        ViewBag.Statuses = new SelectList(
            new[]
            {
                new { Value = HotelBookingStatus.PendingPayment, Text = "بانتظار الدفع" },
                new { Value = HotelBookingStatus.Confirmed, Text = "مؤكد" },
                new { Value = HotelBookingStatus.Cancelled, Text = "ملغي" }
            },
            "Value",
            "Text",
            status);
    }

    private static string GetBookingTypeText(HotelBookingType type) => type switch
    {
        HotelBookingType.Employee => "موظفين",
        HotelBookingType.Pension => "معاشات",
        _ => type.ToString()
    };

    private static string GetStatusText(HotelBookingStatus status) => status switch
    {
        HotelBookingStatus.PendingPayment => "بانتظار الدفع",
        HotelBookingStatus.Confirmed => "مؤكد",
        HotelBookingStatus.Cancelled => "ملغي",
        _ => status.ToString()
    };

    private static string BuildExcelHtml(HotelTripReportsViewModel model)
    {
        var html = new StringBuilder();
        html.AppendLine("<html><head><meta charset='utf-8' /></head><body dir='rtl'>");
        html.AppendLine("<table border='1' style='border-collapse:collapse;width:100%;font-family:Tahoma;'>");
        html.AppendLine("<thead>");
        html.AppendLine("<tr style='background:#f3ead8;font-weight:bold;'>");
        html.AppendLine("<th>الفندق</th><th>المدينة</th><th>الفترة</th><th>الاسم</th><th>الرقم</th><th>القطاع</th><th>التليفون</th><th>بالغين</th><th>أطفال</th><th>مرافقين</th><th>الإجمالي</th><th>نوع الحجز</th><th>الحالة</th>");
        html.AppendLine("</tr>");
        html.AppendLine("</thead><tbody>");

        foreach (var group in model.Groups)
        {
            foreach (var item in group.Items)
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td>{group.HotelName}</td>");
                html.AppendLine($"<td>{group.CityName}</td>");
                html.AppendLine($"<td>{group.StartDate:dd/MM/yyyy} - {group.EndDate:dd/MM/yyyy}</td>");
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
