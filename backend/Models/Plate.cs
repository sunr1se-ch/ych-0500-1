using System.ComponentModel.DataAnnotations;

namespace PlateTracking.Models;

public class Plate
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string SteelPlateNumber { get; set; } = string.Empty;

    [Required]
    public decimal DesignDepth { get; set; }

    [Required]
    public int LifeLimit { get; set; } = 10000;

    public int ImpressionCount { get; set; } = 0;

    public bool IsLocked { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Impression> Impressions { get; set; } = new List<Impression>();
    public ICollection<Warning> Warnings { get; set; } = new List<Warning>();
    public ICollection<AlignmentIncident> AlignmentIncidents { get; set; } = new List<AlignmentIncident>();
}
