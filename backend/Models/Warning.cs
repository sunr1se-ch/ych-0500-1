using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlateTracking.Models;

public class Warning
{
    [Key]
    public int Id { get; set; }

    [Required]
    [ForeignKey(nameof(Plate))]
    public int PlateId { get; set; }

    [Required]
    [MaxLength(50)]
    public string WarningType { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    public bool IsAcknowledged { get; set; } = false;

    public DateTime? AcknowledgedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Plate? Plate { get; set; }
}
