using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Feedback;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public List<Models.Feedback> MyFeedback { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User)!;
        MyFeedback = await _context.Feedbacks
            .Include(f => f.Order)
            .Include(f => f.Reservation)
            .Where(f => f.CustomerId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }
}
