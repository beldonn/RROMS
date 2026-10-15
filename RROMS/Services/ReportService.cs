using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;
using RROMS.ViewModels;

namespace RROMS.Services;

public interface IReportService
{
    Task<SalesReportVM> GetSalesReportAsync(DateTime start, DateTime end);
    Task<List<PopularItemVM>> GetPopularItemsAsync(DateTime start, DateTime end, int top = 10);
    Task<ReservationStatsVM> GetReservationStatsAsync(DateTime start, DateTime end);
    Task<List<CustomerActivityVM>> GetCustomerActivityAsync(DateTime start, DateTime end);
    Task<DashboardVM> GetDashboardDataAsync();
}

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SalesReportVM> GetSalesReportAsync(DateTime start, DateTime end)
    {
        var endExclusive = end.Date.AddDays(1);
        var orders = await _context.Orders
            .Where(o => o.OrderDate >= start.Date && o.OrderDate < endExclusive && o.Status != OrderStatus.Cancelled)
            .ToListAsync();

        var vm = new SalesReportVM
        {
            StartDate = start.Date,
            EndDate = end.Date,
            TotalOrders = orders.Count,
            TotalSubTotal = orders.Sum(o => o.SubTotal),
            TotalTax = orders.Sum(o => o.TaxAmount),
            TotalRevenue = orders.Sum(o => o.TotalAmount),
            AverageOrderValue = orders.Count > 0 ? Math.Round(orders.Average(o => o.TotalAmount), 2) : 0m
        };

        vm.DailyBreakdown = orders
            .GroupBy(o => o.OrderDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailySalesPointVM { Date = g.Key, OrderCount = g.Count(), Revenue = g.Sum(o => o.TotalAmount) })
            .ToList();

        return vm;
    }

    public async Task<List<PopularItemVM>> GetPopularItemsAsync(DateTime start, DateTime end, int top = 10)
    {
        var endExclusive = end.Date.AddDays(1);

        var query = from oi in _context.OrderItems
                     join o in _context.Orders on oi.OrderId equals o.Id
                     join mi in _context.MenuItems on oi.MenuItemId equals mi.Id
                     join mc in _context.MenuCategories on mi.MenuCategoryId equals mc.Id
                     where o.OrderDate >= start.Date && o.OrderDate < endExclusive && o.Status != OrderStatus.Cancelled
                     group oi by new { oi.MenuItemId, ItemName = mi.Name, CategoryName = mc.Name } into g
                     select new PopularItemVM
                     {
                         MenuItemId = g.Key.MenuItemId,
                         Name = g.Key.ItemName,
                         Category = g.Key.CategoryName,
                         QuantitySold = g.Sum(x => x.Quantity),
                         RevenueGenerated = g.Sum(x => x.Subtotal)
                     };

        return await query
            .OrderByDescending(p => p.QuantitySold)
            .Take(top)
            .ToListAsync();
    }

    public async Task<ReservationStatsVM> GetReservationStatsAsync(DateTime start, DateTime end)
    {
        var endExclusive = end.Date.AddDays(1);
        var reservations = await _context.Reservations
            .Include(r => r.Table)
            .Where(r => r.ReservationDateTime >= start.Date && r.ReservationDateTime < endExclusive)
            .ToListAsync();

        var vm = new ReservationStatsVM
        {
            StartDate = start.Date,
            EndDate = end.Date,
            TotalReservations = reservations.Count,
            Confirmed = reservations.Count(r => r.Status == ReservationStatus.Confirmed),
            Completed = reservations.Count(r => r.Status == ReservationStatus.Completed),
            Cancelled = reservations.Count(r => r.Status == ReservationStatus.Cancelled),
            NoShow = reservations.Count(r => r.Status == ReservationStatus.NoShow),
            AveragePartySize = reservations.Count > 0 ? Math.Round(reservations.Average(r => r.PartySize), 1) : 0
        };

        vm.ByHour = reservations
            .GroupBy(r => r.ReservationDateTime.Hour)
            .OrderBy(g => g.Key)
            .Select(g => new HourCountVM { Hour = g.Key, Count = g.Count() })
            .ToList();

        vm.ByTable = reservations
            .Where(r => r.Table != null)
            .GroupBy(r => r.Table!.TableNumber)
            .OrderByDescending(g => g.Count())
            .Select(g => new TableUtilizationVM { TableNumber = g.Key, BookingCount = g.Count() })
            .ToList();

        return vm;
    }

    public async Task<List<CustomerActivityVM>> GetCustomerActivityAsync(DateTime start, DateTime end)
    {
        var endExclusive = end.Date.AddDays(1);

        var customers = await _context.Users
            .Join(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur.RoleId })
            .Join(_context.Roles, x => x.RoleId, r => r.Id, (x, r) => new { x.u, r.Name })
            .Where(x => x.Name == Roles.Customer)
            .Select(x => x.u)
            .ToListAsync();

        var reservations = await _context.Reservations
            .Where(r => r.ReservationDateTime >= start.Date && r.ReservationDateTime < endExclusive)
            .ToListAsync();

        var orders = await _context.Orders
            .Where(o => o.OrderDate >= start.Date && o.OrderDate < endExclusive && o.Status != OrderStatus.Cancelled)
            .ToListAsync();

        var result = new List<CustomerActivityVM>();
        foreach (var c in customers)
        {
            var customerReservations = reservations.Where(r => r.CustomerId == c.Id).ToList();
            var customerOrders = orders.Where(o => o.CustomerId == c.Id).ToList();
            if (customerReservations.Count == 0 && customerOrders.Count == 0) continue;

            var lastVisit = new[] { customerReservations.Select(r => (DateTime?)r.ReservationDateTime).DefaultIfEmpty(null).Max(),
                                     customerOrders.Select(o => (DateTime?)o.OrderDate).DefaultIfEmpty(null).Max() }
                                     .Where(d => d.HasValue).DefaultIfEmpty(null).Max();

            result.Add(new CustomerActivityVM
            {
                CustomerId = c.Id,
                FullName = c.FullName,
                Email = c.Email ?? "",
                ReservationCount = customerReservations.Count,
                OrderCount = customerOrders.Count,
                TotalSpent = customerOrders.Sum(o => o.TotalAmount),
                LastVisit = lastVisit
            });
        }

        return result.OrderByDescending(r => r.TotalSpent).ToList();
    }

    public async Task<DashboardVM> GetDashboardDataAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var sevenDaysAgo = today.AddDays(-6);

        var todayReservations = await _context.Reservations.CountAsync(r => r.ReservationDateTime >= today && r.ReservationDateTime < tomorrow);
        var todayOrders = await _context.Orders.Where(o => o.OrderDate >= today && o.OrderDate < tomorrow).ToListAsync();
        var pendingReservations = await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.Pending);
        var kitchenOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Placed || o.Status == OrderStatus.Preparing);
        var activeTables = await _context.RestaurantTables.CountAsync(t => t.IsActive);

        var sales = await GetSalesReportAsync(sevenDaysAgo, today);
        var reservationStats = await GetReservationStatsAsync(sevenDaysAgo, today);
        var popular = await GetPopularItemsAsync(sevenDaysAgo, today, 6);

        return new DashboardVM
        {
            TodayReservationsCount = todayReservations,
            TodayOrdersCount = todayOrders.Count,
            TodayRevenue = todayOrders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount),
            ActiveTablesCount = activeTables,
            PendingReservationsCount = pendingReservations,
            OrdersInKitchenCount = kitchenOrders,
            SalesLast7Days = sales.DailyBreakdown,
            ReservationsByHour = reservationStats.ByHour,
            TopMenuItems = popular,
            TableBookingFrequency = reservationStats.ByTable.Take(8).ToList()
        };
    }
}
