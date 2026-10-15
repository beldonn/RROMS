namespace RROMS.ViewModels;

public class SalesReportVM
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalSubTotal { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public List<DailySalesPointVM> DailyBreakdown { get; set; } = new();
}

public class DailySalesPointVM
{
    public DateTime Date { get; set; }
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
}

public class PopularItemVM
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal RevenueGenerated { get; set; }
}

public class ReservationStatsVM
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalReservations { get; set; }
    public int Confirmed { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
    public int NoShow { get; set; }
    public double AveragePartySize { get; set; }
    public List<HourCountVM> ByHour { get; set; } = new();
    public List<TableUtilizationVM> ByTable { get; set; } = new();
}

public class HourCountVM
{
    public int Hour { get; set; }
    public int Count { get; set; }
}

public class TableUtilizationVM
{
    public string TableNumber { get; set; } = string.Empty;
    public int BookingCount { get; set; }
}

public class CustomerActivityVM
{
    public string CustomerId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int ReservationCount { get; set; }
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastVisit { get; set; }
}
