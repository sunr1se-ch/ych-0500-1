namespace PlateTracking.Services;

public interface IReportService
{
    Task<byte[]> GeneratePlateReportAsync(int plateId, CancellationToken ct);
}
