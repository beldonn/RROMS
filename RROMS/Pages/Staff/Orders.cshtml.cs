using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RROMS.Models;
using RROMS.Services;

namespace RROMS.Pages.Staff;

public class OrdersModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrdersModel(IOrderService orderService, UserManager<ApplicationUser> userManager)
    {
        _orderService = orderService;
        _userManager = userManager;
    }

    public List<Order> Orders { get; set; } = new();

    public async Task OnGetAsync()
    {
        Orders = await _orderService.GetActiveOrdersAsync();
    }

    public async Task<IActionResult> OnPostAdvanceStatusAsync(int id, OrderStatus newStatus)
    {
        var staffId = _userManager.GetUserId(User)!;
        var (success, message) = await _orderService.UpdateOrderStatusAsync(id, newStatus, staffId);
        TempData[success ? "StatusMessage" : "ErrorMessage"] = message;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var staffId = _userManager.GetUserId(User)!;
        var (success, message) = await _orderService.CancelOrderAsync(id, staffId, isStaffOrAdmin: true);
        TempData[success ? "StatusMessage" : "ErrorMessage"] = message;
        return RedirectToPage();
    }
}
