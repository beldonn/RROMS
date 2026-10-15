using Microsoft.AspNetCore.Mvc.RazorPages;
using RROMS.Services;
using RROMS.ViewModels;

namespace RROMS.Pages.Admin;

public class DashboardModel : PageModel
{
    private readonly IReportService _reportService;

    public DashboardModel(IReportService reportService)
    {
        _reportService = reportService;
    }

    public DashboardVM Data { get; set; } = new();

    public async Task OnGetAsync()
    {
        Data = await _reportService.GetDashboardDataAsync();
    }
}
