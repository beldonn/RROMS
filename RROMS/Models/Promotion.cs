using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RROMS.Models;

public class Promotion
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [StringLength(400)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    [Range(1, 100)]
    public decimal DiscountPercent { get; set; }

    /// <summary>Null means the promotion applies storewide rather than to one dish.</summary>
    public int? MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime EndDate { get; set; } = DateTime.UtcNow.Date.AddDays(7);

    public bool IsActive { get; set; } = true;

    [NotMapped]
    public bool IsCurrentlyRunning => IsActive && DateTime.UtcNow.Date >= StartDate.Date && DateTime.UtcNow.Date <= EndDate.Date;
}
