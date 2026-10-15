namespace RROMS.ViewModels;

public class DashboardVM
{
    public int TodayReservationsCount { get; set; }
    public int TodayOrdersCount { get; set; }
    public decimal TodayRevenue { get; set; }
    public int ActiveTablesCount { get; set; }
    public int PendingReservationsCount { get; set; }
    public int OrdersInKitchenCount { get; set; }

    public List<DailySalesPointVM> SalesLast7Days { get; set; } = new();
    public List<HourCountVM> ReservationsByHour { get; set; } = new();
    public List<PopularItemVM> TopMenuItems { get; set; } = new();
    public List<TableUtilizationVM> TableBookingFrequency { get; set; } = new();
}
