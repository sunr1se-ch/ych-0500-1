using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlateTracking.Models;

public class Impression
{
    [Key]
    public int Id { get; set; }

    [Required]
    [ForeignKey(nameof(Plate))]
    public int PlateId { get; set; }

    public decimal OffsetX { get; set; }

    public decimal OffsetY { get; set; }

    public decimal ActualTemperature { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Plate? Plate { get; set; }
}
