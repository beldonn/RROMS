using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RROMS.Services;
using RROMS.ViewModels;

namespace RROMS.Pages.Admin;

public class ReportsModel : PageModel
{
    private readonly IReportService _reportService;

    public ReportsModel(IReportService reportService)
    {
        _reportService = reportService;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-30);

    [BindProperty(SupportsGet = true)]
    public DateTime EndDate { get; set; } = DateTime.Today;

    public SalesReportVM Sales { get; set; } = new();
    public List<PopularItemVM> PopularItems { get; set; } = new();
    public ReservationStatsVM ReservationStats { get; set; } = new();
    public List<CustomerActivityVM> CustomerActivity { get; set; } = new();

    public async Task OnGetAsync()
    {
        Sales = await _reportService.GetSalesReportAsync(StartDate, EndDate);
        PopularItems = await _reportService.GetPopularItemsAsync(StartDate, EndDate, 15);
        ReservationStats = await _reportService.GetReservationStatsAsync(StartDate, EndDate);
        CustomerActivity = await _reportService.GetCustomerActivityAsync(StartDate, EndDate);
    }

    /// <summary>
    /// One handler serves every export button on the page:
    /// /Admin/Reports?handler=Export&amp;report=sales&amp;format=csv&amp;startDate=...&amp;endDate=...
    /// report: sales | popular | reservations | customers    format: csv | pdf | excel
    /// </summary>
    public async Task<IActionResult> OnGetExportAsync(string report, string format)
    {
        List<string> headers;
        List<List<string>> rows;
        string title;

        switch (report)
        {
            case "popular":
                title = "Popular Menu Items";
                headers = new() { "Item", "Category", "Quantity sold", "Revenue" };
                var popular = await _reportService.GetPopularItemsAsync(StartDate, EndDate, 100);
                rows = popular.Select(p => new List<string> { p.Name, p.Category, p.QuantitySold.ToString(), p.RevenueGenerated.ToString("F2") }).ToList();
                break;

            case "reservations":
                title = "Reservation Statistics";
                headers = new() { "Hour", "Reservation count" };
                var stats = await _reportService.GetReservationStatsAsync(StartDate, EndDate);
                rows = stats.ByHour.Select(h => new List<string> { $"{h.Hour}:00", h.Count.ToString() }).ToList();
                break;

            case "customers":
                title = "Customer Activity";
                headers = new() { "Customer", "Email", "Reservations", "Orders", "Total spent", "Last visit" };
                var activity = await _reportService.GetCustomerActivityAsync(StartDate, EndDate);
                rows = activity.Select(c => new List<string>
                {
                    c.FullName, c.Email, c.ReservationCount.ToString(), c.OrderCount.ToString(),
                    c.TotalSpent.ToString("F2"), c.LastVisit?.ToString("yyyy-MM-dd") ?? ""
                }).ToList();
                break;

            default: // sales
                title = "Sales Report";
                headers = new() { "Date", "Orders", "Revenue" };
                var sales = await _reportService.GetSalesReportAsync(StartDate, EndDate);
                rows = sales.DailyBreakdown.Select(d => new List<string> { d.Date.ToString("yyyy-MM-dd"), d.OrderCount.ToString(), d.Revenue.ToString("F2") }).ToList();
                break;
        }

        var fileNameBase = $"{report}-report-{StartDate:yyyyMMdd}-{EndDate:yyyyMMdd}";

        return format switch
        {
            "pdf" => File(ExportHelper.GeneratePdf(title, headers, rows), "application/pdf", fileNameBase + ".pdf"),
            "excel" => File(ExportHelper.GenerateExcel(title, headers, rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileNameBase + ".xlsx"),
            _ => File(ExportHelper.GenerateCsv(headers, rows), "text/csv", fileNameBase + ".csv")
        };
    }
}
