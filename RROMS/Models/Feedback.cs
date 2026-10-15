using System.ComponentModel.DataAnnotations;

namespace RROMS.Models;

public class Feedback
{
    public int Id { get; set; }

    [Required]
    public string CustomerId { get; set; } = string.Empty;
    public ApplicationUser? Customer { get; set; }

    public int? OrderId { get; set; }
    public Order? Order { get; set; }

    public int? ReservationId { get; set; }
    public Reservation? Reservation { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(1000)]
    public string? Comments { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(600)]
    public string? StaffResponse { get; set; }

    public string? RespondedByStaffId { get; set; }
    public ApplicationUser? RespondedByStaff { get; set; }
    public DateTime? RespondedAt { get; set; }
}
