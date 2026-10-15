using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin;

public class SettingsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public SettingsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public RestaurantSetting Setting { get; set; } = new();
    public List<OperatingHours> Hours { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Setting = await _context.RestaurantSettings.FirstOrDefaultAsync() ?? new RestaurantSetting();
        Hours = await _context.OperatingHours.OrderBy(h => h.DayOfWeek).ToListAsync();
    }

    public async Task<IActionResult> OnPostSaveProfileAsync(string restaurantName, string? address, string? phone, string? email,
        string? description, int reservationDurationMinutes, int maxAdvanceBookingDays, decimal taxRatePercent)
    {
        var setting = await _context.RestaurantSettings.FirstOrDefaultAsync();
        if (setting == null)
        {
            setting = new RestaurantSetting();
            _context.RestaurantSettings.Add(setting);
        }

        setting.RestaurantName = restaurantName;
        setting.Address = address;
        setting.Phone = phone;
        setting.Email = email;
        setting.Description = description;
        setting.ReservationDurationMinutes = reservationDurationMinutes;
        setting.MaxAdvanceBookingDays = maxAdvanceBookingDays;
        setting.TaxRatePercent = taxRatePercent;

        await _context.SaveChangesAsync();
        TempData["StatusMessage"] = "Restaurant settings saved.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSaveHoursAsync(List<int> hourId, List<string> openTime, List<string> closeTime, List<int> closedDayIds)
    {
        var allHours = await _context.OperatingHours.ToListAsync();
        for (var i = 0; i < hourId.Count; i++)
        {
            var record = allHours.FirstOrDefault(h => h.Id == hourId[i]);
            if (record == null) continue;

            record.IsClosed = closedDayIds.Contains(hourId[i]);
            if (TimeSpan.TryParse(openTime[i], out var open)) record.OpenTime = open;
            if (TimeSpan.TryParse(closeTime[i], out var close)) record.CloseTime = close;
        }
        await _context.SaveChangesAsync();
        TempData["StatusMessage"] = "Operating hours updated.";
        return RedirectToPage();
    }
}
