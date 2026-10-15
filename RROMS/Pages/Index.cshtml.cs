using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;
using RROMS.Services;

namespace RROMS.Pages;

[AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IRecommendationService _recommendationService;

    public IndexModel(ApplicationDbContext context, IRecommendationService recommendationService)
    {
        _context = context;
        _recommendationService = recommendationService;
    }

    public RestaurantSetting? Setting { get; set; }
    public List<Announcement> Announcements { get; set; } = new();
    public List<Promotion> ActivePromotions { get; set; } = new();
    public List<MenuItem> FeaturedItems { get; set; } = new();
    public List<OperatingHours> Hours { get; set; } = new();

    public async Task OnGetAsync()
    {
        Setting = await _context.RestaurantSettings.AsNoTracking().FirstOrDefaultAsync();

        Announcements = await _context.Announcements
            .Where(a => a.IsActive && (a.ExpiresAt == null || a.ExpiresAt >= DateTime.UtcNow))
            .OrderByDescending(a => a.CreatedAt)
            .Take(3)
            .ToListAsync();

        ActivePromotions = await _context.Promotions
            .Include(p => p.MenuItem)
            .Where(p => p.IsActive && p.StartDate <= DateTime.UtcNow.Date && p.EndDate >= DateTime.UtcNow.Date)
            .ToListAsync();

        FeaturedItems = await _recommendationService.GetPopularItemsAsync(6);

        Hours = await _context.OperatingHours.OrderBy(h => h.DayOfWeek).ToListAsync();
    }
}
