using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;
using RROMS.Services;

namespace RROMS.Pages.Admin;

public class PromotionsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public PromotionsModel(ApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public List<Promotion> Promotions { get; set; } = new();
    public List<MenuItem> MenuItems { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Promotions = await _context.Promotions.Include(p => p.MenuItem).OrderByDescending(p => p.StartDate).ToListAsync();
        MenuItems = await _context.MenuItems.OrderBy(m => m.Name).ToListAsync();
    }

    public async Task<IActionResult> OnPostAddAsync(string title, string? description, decimal discountPercent, int? menuItemId, DateTime startDate, DateTime endDate, bool notifyCustomers)
    {
        var promo = new Promotion
        {
            Title = title,
            Description = description,
            DiscountPercent = discountPercent,
            MenuItemId = menuItemId,
            StartDate = startDate,
            EndDate = endDate,
            IsActive = true
        };
        _context.Promotions.Add(promo);
        await _context.SaveChangesAsync();

        if (notifyCustomers)
        {
            await _notificationService.NotifyAllCustomersAsync(
                "New promotion: " + title,
                description ?? $"{discountPercent}% off, running {startDate:dd MMM} - {endDate:dd MMM}.",
                NotificationType.Promotion,
                "/Menu/Index");
        }

        TempData["StatusMessage"] = "Promotion added.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync(int id, string title, string? description, decimal discountPercent, int? menuItemId, DateTime startDate, DateTime endDate, bool isActive)
    {
        var promo = await _context.Promotions.FindAsync(id);
        if (promo == null) { TempData["ErrorMessage"] = "Promotion not found."; return RedirectToPage(); }

        promo.Title = title;
        promo.Description = description;
        promo.DiscountPercent = discountPercent;
        promo.MenuItemId = menuItemId;
        promo.StartDate = startDate;
        promo.EndDate = endDate;
        promo.IsActive = isActive;
        await _context.SaveChangesAsync();
        TempData["StatusMessage"] = "Promotion updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var promo = await _context.Promotions.FindAsync(id);
        if (promo != null)
        {
            _context.Promotions.Remove(promo);
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Promotion removed.";
        }
        return RedirectToPage();
    }
}
