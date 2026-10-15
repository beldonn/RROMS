using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RROMS.Models;
using RROMS.Services;

namespace RROMS.Pages.Staff;

public class ReservationsModel : PageModel
{
    private readonly IReservationService _reservationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReservationsModel(IReservationService reservationService, UserManager<ApplicationUser> userManager)
    {
        _reservationService = reservationService;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime Date { get; set; } = DateTime.Today;

    public List<Reservation> Reservations { get; set; } = new();

    public async Task OnGetAsync()
    {
        Reservations = await _reservationService.GetReservationsForDateAsync(Date);
    }

    public async Task<IActionResult> OnPostConfirmAsync(int id)
    {
        var staffId = _userManager.GetUserId(User)!;
        var (success, message) = await _reservationService.ConfirmReservationAsync(id, staffId, null);
        TempData[success ? "StatusMessage" : "ErrorMessage"] = message;
        return RedirectToPage(new { Date });
    }

    public async Task<IActionResult> OnPostSetStatusAsync(int id, ReservationStatus status)
    {
        var staffId = _userManager.GetUserId(User)!;
        var (success, message) = await _reservationService.UpdateStatusAsync(id, status, staffId);
        TempData[success ? "StatusMessage" : "ErrorMessage"] = message;
        return RedirectToPage(new { Date });
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var staffId = _userManager.GetUserId(User)!;
        var (success, message) = await _reservationService.CancelReservationAsync(id, staffId, isStaffOrAdmin: true, reason: "Cancelled by staff");
        TempData[success ? "StatusMessage" : "ErrorMessage"] = message;
        return RedirectToPage(new { Date });
    }
}
