using System.Globalization;
using System.Text;
using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities;
using ECM.ReservationSystem.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ECM.ReservationSystem.Controllers.Admin;

[Route("Admin/[controller]")]
[Authorize]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? cityId, int? unitTypeId, int? year, string? weekId, ReservationStatus? status)
    {
        var targetYear = year ?? DateTime.Now.Year;
        var model = await BuildReportsModelAsync(cityId, unitTypeId, targetYear, weekId, status);
        await PopulateFiltersAsync(cityId, unitTypeId, targetYear, weekId, status);
        return View(model);
    }

    [HttpGet("ExportExcel")]
    public async Task<IActionResult> ExportExcel(int? cityId, int? unitTypeId, int? year, string? weekId, ReservationStatus? status)
    {
        var targetYear = year ?? DateTime.Now.Year;
        var model = await BuildReportsModelAsync(cityId, unitTypeId, targetYear, weekId, status);
        var content = BuildExcelHtml(model);
        var fileName = $"reservation-reports-{DateTime.Now:yyyyMMdd-HHmmss}.xls";
        return File(Encoding.UTF8.GetBytes(content), "application/vnd.ms-excel; charset=utf-8", fileName);
    }

    [HttpGet("ExportPdf")]
    public async Task<IActionResult> ExportPdf(int? cityId, int? unitTypeId, int? year, string? weekId, ReservationStatus? status)
    {
        var targetYear = year ?? DateTime.Now.Year;
        var model = await BuildReportsModelAsync(cityId, unitTypeId, targetYear, weekId, status);
        return View("Print", model);
    }

    private async Task<ReportsIndexViewModel> BuildReportsModelAsync(int? cityId, int? unitTypeId, int targetYear, string? weekId, ReservationStatus? status)
    {
        var query = _context.Reservations
            .Include(r => r.Unit)
            .ThenInclude(u => u.City)
            .Include(r => r.Unit)
            .ThenInclude(u => u.UnitType)
            .Where(r => r.CheckInDate.Year == targetYear)
            .AsQueryable();

        if (cityId.HasValue)
        {
            query = query.Where(r => r.Unit.CityId == cityId.Value);
        }

        if (unitTypeId.HasValue)
        {
            query = query.Where(r => r.Unit.UnitTypeId == unitTypeId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (TryParseWeekId(weekId, out var weekCityId, out var weekStartDate))
        {
            query = query.Where(r => r.Unit.CityId == weekCityId && r.CheckInDate.Date == weekStartDate.Date);
        }

        var reservations = await query
            .OrderBy(r => r.Unit.City!.Name)
            .ThenBy(r => r.CheckInDate)
            .ThenBy(r => r.Unit.UnitType!.Name)
            .ThenBy(r => r.Unit.Code)
            .ThenBy(r => r.EmployeeName)
            .ToListAsync();

        return new ReportsIndexViewModel
        {
            Year = targetYear,
            CityId = cityId,
            UnitTypeId = unitTypeId,
            WeekId = weekId,
            Status = status,
            Groups = reservations
                .GroupBy(r => new
                {
                    CityName = r.Unit.City!.NameAr ?? r.Unit.City.Name,
                    r.CheckInDate,
                    r.CheckOutDate
                })
                .Select(group => new ReportsReservationGroupViewModel
                {
                    CityName = group.Key.CityName,
                    WeekStartDate = group.Key.CheckInDate,
                    WeekEndDate = group.Key.CheckOutDate,
                    ReservationCount = group.Count(),
                    PassengerCount = group.Sum(r => r.NumberOfGuests),
                    TotalAmount = group.Sum(r => r.TotalAmount),
                    Items = group.Select(r => new ReportsReservationItemViewModel
                    {
                        ReservationId = r.Id,
                        EmployeeName = r.EmployeeName,
                        EmployeeNumber = r.EmployeeNumber,
                        PhoneNumber = r.PhoneNumber,
                        UnitCode = r.Unit.Code ?? string.Empty,
                        UnitName = r.Unit.Name,
                        UnitTypeName = r.Unit.UnitType!.NameAr ?? r.Unit.UnitType.Name,
                        NumberOfGuests = r.NumberOfGuests,
                        IsTransportationRequired = r.IsTransportationRequired,
                        TotalAmount = r.TotalAmount,
                        Status = GetStatusText(r.Status)
                    }).ToList()
                })
                .OrderBy(g => g.WeekStartDate)
                .ThenBy(g => g.CityName)
                .ToList()
        };
    }

    private async Task PopulateFiltersAsync(int? cityId, int? unitTypeId, int targetYear, string? weekId, ReservationStatus? status)
    {
        ViewBag.Cities = new SelectList(await _context.Cities.Where(c => c.IsActive).ToListAsync(), "Id", "Name", cityId);
        ViewBag.UnitTypes = new SelectList(await _context.UnitTypes.Where(ut => ut.IsActive).ToListAsync(), "Id", "Name", unitTypeId);
        ViewBag.Years = new SelectList(
            GetAvailableYears().Select(y => new { Value = y, Text = y }),
            "Value",
            "Text",
            targetYear);
        ViewBag.Statuses = new SelectList(
            new[]
            {
                new { Value = ReservationStatus.TemporaryHold, Text = "حجز مؤقت" },
                new { Value = ReservationStatus.Approved, Text = "مؤكد" },
                new { Value = ReservationStatus.Cancelled, Text = "ملغي" }
            },
            "Value",
            "Text",
            status);
        ViewBag.Weeks = new SelectList(await BuildWeekOptionsAsync(targetYear, cityId, unitTypeId), "Id", "DisplayName", weekId);
    }

    private async Task<List<object>> BuildWeekOptionsAsync(int year, int? cityId, int? unitTypeId)
    {
        var slotQuery = _context.UnitScheduleSlots
            .Include(s => s.Unit)
            .ThenInclude(u => u!.City)
            .Include(s => s.Unit)
            .ThenInclude(u => u!.UnitType)
            .Where(s => s.IsActive && s.Unit.IsActive && s.SlotStartDate.Year == year)
            .AsQueryable();

        if (cityId.HasValue)
        {
            slotQuery = slotQuery.Where(s => s.Unit.CityId == cityId.Value);
        }

        if (unitTypeId.HasValue)
        {
            slotQuery = slotQuery.Where(s => s.Unit.UnitTypeId == unitTypeId.Value);
        }

        var slots = await slotQuery
            .OrderBy(s => s.Unit.City!.Name)
            .ThenBy(s => s.SlotStartDate)
            .Select(s => new
            {
                s.Unit.CityId,
                CityName = s.Unit.City!.NameAr ?? s.Unit.City.Name,
                s.SlotStartDate,
                s.SlotEndDate
            })
            .Distinct()
            .ToListAsync();

        return slots
            .Select(s => new
            {
                Id = $"{s.CityId}-{s.SlotStartDate:yyyyMMdd}",
                DisplayName = $"{s.CityName} | {s.SlotStartDate:dd/MM/yyyy} - {s.SlotEndDate:dd/MM/yyyy}"
            })
            .Cast<object>()
            .ToList();
    }

    private static string GetStatusText(ReservationStatus status)
    {
        return status switch
        {
            ReservationStatus.TemporaryHold => "حجز مؤقت",
            ReservationStatus.Approved => "مؤكد",
            ReservationStatus.Cancelled => "ملغي",
            ReservationStatus.Confirmed => "مؤكد",
            ReservationStatus.Paid => "مدفوع",
            _ => status.ToString()
        };
    }

    private static string BuildExcelHtml(ReportsIndexViewModel model)
    {
        var html = new StringBuilder();
        html.AppendLine("<html><head><meta charset='utf-8' /></head><body dir='rtl'>");
        html.AppendLine("<table border='1' style='border-collapse:collapse;width:100%;font-family:Tahoma;'>");
        html.AppendLine("<thead>");
        html.AppendLine("<tr style='background:#f3ead8;font-weight:bold;'>");
        html.AppendLine("<th>المدينة</th><th>الفوج</th><th>الموظف</th><th>رقم العامل</th><th>الهاتف</th><th>رقم الوحدة</th><th>نوع الوحدة</th><th>عدد الأفراد</th><th>الإجمالي</th><th>الحالة</th>");
        html.AppendLine("</tr>");
        html.AppendLine("</thead><tbody>");

        foreach (var group in model.Groups)
        {
            foreach (var item in group.Items)
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td>{group.CityName}</td>");
                html.AppendLine($"<td>{group.WeekStartDate:dd/MM/yyyy} - {group.WeekEndDate:dd/MM/yyyy}</td>");
                html.AppendLine($"<td>{item.EmployeeName}</td>");
                html.AppendLine($"<td>{item.EmployeeNumber}</td>");
                html.AppendLine($"<td>{item.PhoneNumber}</td>");
                html.AppendLine($"<td>{(string.IsNullOrWhiteSpace(item.UnitCode) ? item.UnitName : item.UnitCode)}</td>");
                html.AppendLine($"<td>{item.UnitTypeName}</td>");
                html.AppendLine($"<td>{item.NumberOfGuests}</td>");
                html.AppendLine($"<td>{item.TotalAmount:N2}</td>");
                html.AppendLine($"<td>{item.Status}</td>");
                html.AppendLine("</tr>");
            }
        }

        html.AppendLine("</tbody></table></body></html>");
        return html.ToString();
    }

    private static bool TryParseWeekId(string? weekId, out int cityId, out DateTime weekStartDate)
    {
        cityId = 0;
        weekStartDate = default;

        if (string.IsNullOrWhiteSpace(weekId))
        {
            return false;
        }

        var parts = weekId.Split('-', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2
               && int.TryParse(parts[0], out cityId)
               && DateTime.TryParseExact(parts[1], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out weekStartDate);
    }

    private static List<int> GetAvailableYears()
    {
        var currentYear = DateTime.Now.Year;
        return Enumerable.Range(currentYear - 2, 5).ToList();
    }
}
