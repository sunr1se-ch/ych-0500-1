using System.ComponentModel.DataAnnotations;

namespace PlateTracking.Dtos;

public record CreatePlateDto(
    [Required][MaxLength(50)] string SteelPlateNumber,
    [Required] decimal DesignDepth,
    int LifeLimit = 10000
);

public record PlateSummaryDto(
    int Id,
    string SteelPlateNumber,
    decimal DesignDepth,
    int LifeLimit,
    int ImpressionCount,
    bool IsLocked,
    decimal LifePercentage,
    DateTime CreatedAt
);

public record PlateDetailDto(
    int Id,
    string SteelPlateNumber,
    decimal DesignDepth,
    int LifeLimit,
    int ImpressionCount,
    bool IsLocked,
    decimal LifePercentage,
    DateTime CreatedAt,
    List<ImpressionDto> Impressions,
    List<WarningDto> Warnings,
    List<IncidentDto> Incidents
);

public record CreateImpressionDto(
    [Required] int PlateId,
    decimal OffsetX,
    decimal OffsetY,
    decimal ActualTemperature
);

public record ImpressionDto(
    int Id,
    int PlateId,
    decimal OffsetX,
    decimal OffsetY,
    decimal ActualTemperature,
    DateTime CreatedAt
);

public record WarningDto(
    int Id,
    int PlateId,
    string WarningType,
    string Message,
    bool IsAcknowledged,
    DateTime CreatedAt
);

public record IncidentDto(
    int Id,
    int PlateId,
    string SteelPlateNumber,
    int StartImpressionId,
    int EndImpressionId,
    string Axis,
    string? Notes,
    bool IsResolved,
    DateTime CreatedAt
);

public record ResolveIncidentDto(
    [MaxLength(500)] string? Notes
);

public record AcknowledgeWarningDto();
