using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;
using RROMS.Services;

namespace RROMS.Pages.Staff;

public class BillingModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IOrderService _orderService;
    private readonly UserManager<ApplicationUser> _userManager;

    public BillingModel(ApplicationDbContext context, IOrderService orderService, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _orderService = orderService;
        _userManager = userManager;
    }

    public List<Order> PendingPaymentOrders { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? SearchOrderId { get; set; }

    public Order? SearchResult { get; set; }

    public async Task OnGetAsync()
    {
        PendingPaymentOrders = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Payment)
            .Include(o => o.OrderItems)
            .Where(o => o.Payment != null && o.Payment.Status == PaymentStatus.Pending)
            .OrderBy(o => o.OrderDate)
            .ToListAsync();

        if (SearchOrderId.HasValue)
            SearchResult = await _orderService.GetByIdAsync(SearchOrderId.Value);
    }

    public async Task<IActionResult> OnPostRecordPaymentAsync(int orderId, PaymentMethod method)
    {
        var staffId = _userManager.GetUserId(User)!;
        var (success, message) = await _orderService.RecordPaymentAsync(orderId, method, staffId, null);
        TempData[success ? "StatusMessage" : "ErrorMessage"] = message;
        return RedirectToPage();
    }
}
