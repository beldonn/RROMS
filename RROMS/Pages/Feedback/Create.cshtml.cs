using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Feedback;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [BindProperty]
    public FeedbackInputModel Input { get; set; } = new();

    public List<Order> CompletedOrders { get; set; } = new();
    public List<Reservation> PastReservations { get; set; } = new();

    public async Task OnGetAsync(int? orderId, int? reservationId)
    {
        await LoadOptionsAsync();
        if (orderId.HasValue) Input.OrderId = orderId;
        if (reservationId.HasValue) Input.ReservationId = reservationId;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadOptionsAsync();
        if (!ModelState.IsValid) return Page();

        var userId = _userManager.GetUserId(User)!;
        var feedback = new Models.Feedback
        {
            CustomerId = userId,
            OrderId = Input.OrderId,
            ReservationId = Input.ReservationId,
            Rating = Input.Rating,
            Comments = Input.Comments,
            CreatedAt = DateTime.UtcNow
        };

        _context.Feedbacks.Add(feedback);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Thank you for your feedback!";
        return RedirectToPage("/Feedback/Index");
    }

    private async Task LoadOptionsAsync()
    {
        var userId = _userManager.GetUserId(User)!;
        CompletedOrders = await _context.Orders
            .Where(o => o.CustomerId == userId && o.Status == OrderStatus.Completed)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
        PastReservations = await _context.Reservations
            .Where(r => r.CustomerId == userId && r.Status == ReservationStatus.Completed)
            .OrderByDescending(r => r.ReservationDateTime)
            .ToListAsync();
    }

    public class FeedbackInputModel
    {
        public int? OrderId { get; set; }
        public int? ReservationId { get; set; }

        [Required, Range(1, 5, ErrorMessage = "Please choose a rating from 1 to 5.")]
        public int Rating { get; set; } = 5;

        [StringLength(1000)]
        public string? Comments { get; set; }
    }
}
