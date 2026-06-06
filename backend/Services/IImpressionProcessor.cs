using PlateTracking.Dtos;
using PlateTracking.Models;

namespace PlateTracking.Services;

public interface IImpressionProcessor
{
    Task<(Impression Impression, List<Warning> Warnings, List<AlignmentIncident> Incidents)> ProcessImpressionAsync(
        Plate plate, CreateImpressionDto dto, CancellationToken ct);
}
