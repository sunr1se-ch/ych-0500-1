using Microsoft.EntityFrameworkCore;
using PlateTracking.Data;
using PlateTracking.Dtos;
using PlateTracking.Models;

namespace PlateTracking.Services;

public class ImpressionProcessor(AppDbContext db) : IImpressionProcessor
{
    private const decimal AlignmentThreshold = 0.08m;
    private const int ConsecutiveFailureCount = 3;
    private const decimal WarningThreshold = 0.80m;

    public async Task<(Impression Impression, List<Warning> Warnings, List<AlignmentIncident> Incidents)> ProcessImpressionAsync(
        Plate plate, CreateImpressionDto dto, CancellationToken ct)
    {
        var warnings = new List<Warning>();
        var incidents = new List<AlignmentIncident>();

        var impression = new Impression
        {
            PlateId = plate.Id,
            OffsetX = dto.OffsetX,
            OffsetY = dto.OffsetY,
            ActualTemperature = dto.ActualTemperature,
            CreatedAt = DateTime.UtcNow
        };

        db.Impressions.Add(impression);
        plate.ImpressionCount++;

        var lifePercentage = (decimal)plate.ImpressionCount / plate.LifeLimit;

        if (lifePercentage >= 1.0m && !plate.IsLocked)
        {
            plate.IsLocked = true;
            warnings.Add(new Warning
            {
                PlateId = plate.Id,
                WarningType = "LifeExceeded",
                Message = $"版材已达到寿命极限 {plate.LifeLimit} 次，已自动锁定。当前压印次数: {plate.ImpressionCount}"
            });
        }
        else if (lifePercentage >= WarningThreshold)
        {
            var existingWarning = await db.Warnings
                .AnyAsync(w => w.PlateId == plate.Id
                    && w.WarningType == "LifeWarning"
                    && !w.IsAcknowledged, ct);

            if (!existingWarning)
            {
                warnings.Add(new Warning
                {
                    PlateId = plate.Id,
                    WarningType = "LifeWarning",
                    Message = $"版材寿命已达 {lifePercentage:P0}，当前压印次数: {plate.ImpressionCount}/{plate.LifeLimit}"
                });
            }
        }

        var recentImpressions = await db.Impressions
            .Where(i => i.PlateId == plate.Id)
            .OrderByDescending(i => i.CreatedAt)
            .Take(ConsecutiveFailureCount)
            .ToListAsync(ct);

        recentImpressions.Add(impression);
        recentImpressions = recentImpressions
            .OrderByDescending(i => i.CreatedAt)
            .Take(ConsecutiveFailureCount)
            .ToList();

        if (recentImpressions.Count >= ConsecutiveFailureCount)
        {
            await CheckAlignmentIncidentsAsync(plate, recentImpressions, "X", i => i.OffsetX, incidents, ct);
            await CheckAlignmentIncidentsAsync(plate, recentImpressions, "Y", i => i.OffsetY, incidents, ct);
        }

        return (impression, warnings, incidents);
    }

    private async Task CheckAlignmentIncidentsAsync(
        Plate plate,
        List<Impression> recentImpressions,
        string axis,
        Func<Impression, decimal> selector,
        List<AlignmentIncident> incidents,
        CancellationToken ct)
    {
        var allExceed = recentImpressions.All(i => Math.Abs(selector(i)) > AlignmentThreshold);

        if (!allExceed) return;

        var existingOpenIncident = await db.AlignmentIncidents
            .AnyAsync(a => a.PlateId == plate.Id
                && a.Axis == axis
                && !a.IsResolved, ct);

        if (existingOpenIncident) return;

        var ordered = recentImpressions.OrderBy(i => i.CreatedAt).ToList();
        incidents.Add(new AlignmentIncident
        {
            PlateId = plate.Id,
            StartImpressionId = ordered[0].Id,
            EndImpressionId = ordered[^1].Id,
            Axis = axis,
            Notes = $"连续 {ConsecutiveFailureCount} 次 {axis} 轴偏移超过 {AlignmentThreshold} μm"
        });
    }
}
