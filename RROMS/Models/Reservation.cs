using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RROMS.Models;

public class Reservation
{
    public int Id { get; set; }

    [Required]
    public string CustomerId { get; set; } = string.Empty;
    public ApplicationUser? Customer { get; set; }

    public int? TableId { get; set; }
    public RestaurantTable? Table { get; set; }

    [Range(1, 30)]
    public int PartySize { get; set; }

    /// <summary>Combined booking date + time, stored in local restaurant time.</summary>
    public DateTime ReservationDateTime { get; set; }

    public int DurationMinutes { get; set; } = 90;

    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    [StringLength(400)]
    public string? SpecialRequests { get; set; }

    [StringLength(300)]
    public string? CancellationReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }

    public string? ConfirmedByStaffId { get; set; }
    public ApplicationUser? ConfirmedByStaff { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    [NotMapped]
    public DateTime ReservationEndDateTime => ReservationDateTime.AddMinutes(DurationMinutes);
}
