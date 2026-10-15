using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin;

public class MonitoringModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public MonitoringModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-7);

    [BindProperty(SupportsGet = true)]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);

    [BindProperty(SupportsGet = true)]
    public string? ReservationStatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? OrderStatusFilter { get; set; }

    public List<Reservation> Reservations { get; set; } = new();
    public List<Order> Orders { get; set; } = new();

    public async Task OnGetAsync()
    {
        var endExclusive = EndDate.Date.AddDays(1);

        var reservationQuery = _context.Reservations
            .Include(r => r.Customer)
            .Include(r => r.Table).ThenInclude(t => t!.DiningArea)
            .Where(r => r.ReservationDateTime >= StartDate.Date && r.ReservationDateTime < endExclusive);
        if (!string.IsNullOrEmpty(ReservationStatusFilter) && Enum.TryParse<ReservationStatus>(ReservationStatusFilter, out var rStatus))
            reservationQuery = reservationQuery.Where(r => r.Status == rStatus);
        Reservations = await reservationQuery.OrderByDescending(r => r.ReservationDateTime).ToListAsync();

        var orderQuery = _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .Include(o => o.Payment)
            .Where(o => o.OrderDate >= StartDate.Date && o.OrderDate < endExclusive);
        if (!string.IsNullOrEmpty(OrderStatusFilter) && Enum.TryParse<OrderStatus>(OrderStatusFilter, out var oStatus))
            orderQuery = orderQuery.Where(o => o.Status == oStatus);
        Orders = await orderQuery.OrderByDescending(o => o.OrderDate).ToListAsync();
    }
}
