using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlateTracking.Models;

public class AlignmentIncident
{
    [Key]
    public int Id { get; set; }

    [Required]
    [ForeignKey(nameof(Plate))]
    public int PlateId { get; set; }

    [Required]
    public int StartImpressionId { get; set; }

    [Required]
    public int EndImpressionId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Axis { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsResolved { get; set; } = false;

    public DateTime? ResolvedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Plate? Plate { get; set; }
}
