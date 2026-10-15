using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Services;

public interface IRecommendationService
{
    Task<List<MenuItem>> GetRecommendationsAsync(string? customerId, int count = 6);
    Task<List<MenuItem>> GetPopularItemsAsync(int count = 6);
}

/// <summary>
/// Two-stage recommendation logic:
/// 1) Personalised - looks at the categories a customer has ordered from before and
///    suggests other available, well-liked dishes from those categories they haven't tried.
/// 2) Popularity fallback - used for guests, brand-new customers, or to top up the list -
///    ranks dishes by total quantity sold across all orders.
/// </summary>
public class RecommendationService : IRecommendationService
{
    private readonly ApplicationDbContext _context;

    public RecommendationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MenuItem>> GetRecommendationsAsync(string? customerId, int count = 6)
    {
        var popularity = await GetItemPopularityAsync();

        if (string.IsNullOrEmpty(customerId))
            return await GetPopularItemsAsync(count);

        var orderedItemIds = await _context.OrderItems
            .Where(oi => oi.Order!.CustomerId == customerId)
            .Select(oi => oi.MenuItemId)
            .Distinct()
            .ToListAsync();

        if (orderedItemIds.Count == 0)
            return await GetPopularItemsAsync(count);

        var orderedCategoryIds = await _context.MenuItems
            .Where(m => orderedItemIds.Contains(m.Id))
            .Select(m => m.MenuCategoryId)
            .Distinct()
            .ToListAsync();

        var candidates = await _context.MenuItems
            .Include(m => m.MenuCategory)
            .Where(m => m.IsAvailable && orderedCategoryIds.Contains(m.MenuCategoryId) && !orderedItemIds.Contains(m.Id))
            .ToListAsync();

        var recommended = candidates
            .OrderByDescending(m => popularity.TryGetValue(m.Id, out var qty) ? qty : 0)
            .Take(count)
            .ToList();

        if (recommended.Count < count)
        {
            var fillerNeeded = count - recommended.Count;
            var alreadyChosenIds = recommended.Select(m => m.Id).Concat(orderedItemIds).ToHashSet();
            var filler = await _context.MenuItems
                .Include(m => m.MenuCategory)
                .Where(m => m.IsAvailable && !alreadyChosenIds.Contains(m.Id))
                .ToListAsync();

            var fillerRanked = filler
                .OrderByDescending(m => popularity.TryGetValue(m.Id, out var qty) ? qty : 0)
                .Take(fillerNeeded);

            recommended.AddRange(fillerRanked);
        }

        return recommended;
    }

    public async Task<List<MenuItem>> GetPopularItemsAsync(int count = 6)
    {
        var popularity = await GetItemPopularityAsync();

        var available = await _context.MenuItems
            .Include(m => m.MenuCategory)
            .Where(m => m.IsAvailable)
            .ToListAsync();

        return available
            .OrderByDescending(m => popularity.TryGetValue(m.Id, out var qty) ? qty : 0)
            .ThenBy(m => m.Name)
            .Take(count)
            .ToList();
    }

    private async Task<Dictionary<int, int>> GetItemPopularityAsync()
    {
        return await _context.OrderItems
            .GroupBy(oi => oi.MenuItemId)
            .Select(g => new { MenuItemId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.MenuItemId, x => x.Quantity);
    }
}
