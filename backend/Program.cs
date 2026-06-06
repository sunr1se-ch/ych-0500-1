using Microsoft.EntityFrameworkCore;
using PlateTracking.Data;
using PlateTracking.Dtos;
using PlateTracking.Models;
using PlateTracking.Services;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=plate_tracking.db"));

builder.Services.AddScoped<IImpressionProcessor, ImpressionProcessor>();
builder.Services.AddScoped<IReportService, ReportService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();

app.MapGet("/api/plates", async (AppDbContext db, CancellationToken ct) =>
{
    var plates = await db.Plates
        .OrderByDescending(p => p.CreatedAt)
        .Select(p => new PlateSummaryDto(
            p.Id,
            p.SteelPlateNumber,
            p.DesignDepth,
            p.LifeLimit,
            p.ImpressionCount,
            p.IsLocked,
            p.LifeLimit > 0 ? Math.Round((decimal)p.ImpressionCount / p.LifeLimit * 100, 2) : 0,
            p.CreatedAt
        ))
        .ToListAsync(ct);
    return Results.Ok(plates);
})
.WithName("GetPlates")
.WithOpenApi();

app.MapGet("/api/plates/{id}", async (int id, AppDbContext db, CancellationToken ct) =>
{
    var plate = await db.Plates
        .Include(p => p.Impressions.OrderByDescending(i => i.CreatedAt))
        .Include(p => p.Warnings.OrderByDescending(w => w.CreatedAt))
        .Include(p => p.AlignmentIncidents.OrderByDescending(a => a.CreatedAt))
        .FirstOrDefaultAsync(p => p.Id == id, ct);

    if (plate == null)
        return Results.NotFound($"Plate with id {id} not found");

    var dto = new PlateDetailDto(
        plate.Id,
        plate.SteelPlateNumber,
        plate.DesignDepth,
        plate.LifeLimit,
        plate.ImpressionCount,
        plate.IsLocked,
        plate.LifeLimit > 0 ? Math.Round((decimal)plate.ImpressionCount / plate.LifeLimit * 100, 2) : 0,
        plate.CreatedAt,
        plate.Impressions.Select(i => new ImpressionDto(
            i.Id,
            i.PlateId,
            i.OffsetX,
            i.OffsetY,
            i.ActualTemperature,
            i.CreatedAt
        )).ToList(),
        plate.Warnings.Select(w => new WarningDto(
            w.Id,
            w.PlateId,
            w.WarningType,
            w.Message,
            w.IsAcknowledged,
            w.CreatedAt
        )).ToList(),
        plate.AlignmentIncidents.Select(a => new IncidentDto(
            a.Id,
            a.PlateId,
            plate.SteelPlateNumber,
            a.StartImpressionId,
            a.EndImpressionId,
            a.Axis,
            a.Notes,
            a.IsResolved,
            a.CreatedAt
        )).ToList()
    );

    return Results.Ok(dto);
})
.WithName("GetPlateById")
.WithOpenApi();

app.MapGet("/api/plates/{id}/report", async (int id, IReportService reportService, CancellationToken ct) =>
{
    try
    {
        var pdfData = await reportService.GeneratePlateReportAsync(id, ct);
        return Results.File(pdfData, "application/pdf", $"plate_{id}_report.pdf");
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(ex.Message);
    }
})
.WithName("GetPlateReport")
.WithOpenApi();

app.MapPost("/api/plates", async (CreatePlateDto dto, AppDbContext db, CancellationToken ct) =>
{
    var plate = new Plate
    {
        SteelPlateNumber = dto.SteelPlateNumber,
        DesignDepth = dto.DesignDepth,
        LifeLimit = dto.LifeLimit > 0 ? dto.LifeLimit : 10000,
        ImpressionCount = 0,
        IsLocked = false,
        CreatedAt = DateTime.UtcNow
    };

    db.Plates.Add(plate);
    await db.SaveChangesAsync(ct);

    return Results.CreatedAtRoute("GetPlateById", new { id = plate.Id }, plate);
})
.WithName("CreatePlate")
.WithOpenApi();

app.MapPost("/api/impressions", async (CreateImpressionDto dto, AppDbContext db, IImpressionProcessor processor, CancellationToken ct) =>
{
    var plate = await db.Plates.FirstOrDefaultAsync(p => p.Id == dto.PlateId, ct);
    if (plate == null)
        return Results.NotFound($"Plate with id {dto.PlateId} not found");

    if (plate.IsLocked)
        return Results.BadRequest($"Plate {plate.SteelPlateNumber} is locked due to life limit exceeded");

    var (impression, warnings, incidents) = await processor.ProcessImpressionAsync(plate, dto, ct);

    if (warnings.Any())
        db.Warnings.AddRange(warnings);

    if (incidents.Any())
        db.AlignmentIncidents.AddRange(incidents);

    if (warnings.Any() || incidents.Any())
        await db.SaveChangesAsync(ct);

    var result = new
    {
        Impression = new ImpressionDto(
            impression.Id,
            impression.PlateId,
            impression.OffsetX,
            impression.OffsetY,
            impression.ActualTemperature,
            impression.CreatedAt
        ),
        Warnings = warnings.Select(w => new WarningDto(
            w.Id,
            w.PlateId,
            w.WarningType,
            w.Message,
            w.IsAcknowledged,
            w.CreatedAt
        )).ToList(),
        Incidents = incidents.Select(i => new IncidentDto(
            i.Id,
            i.PlateId,
            plate.SteelPlateNumber,
            i.StartImpressionId,
            i.EndImpressionId,
            i.Axis,
            i.Notes,
            i.IsResolved,
            i.CreatedAt
        )).ToList(),
        PlateLocked = plate.IsLocked
    };

    return Results.Ok(result);
})
.WithName("CreateImpression")
.WithOpenApi();

app.MapGet("/api/incidents", async (AppDbContext db, CancellationToken ct) =>
{
    var incidents = await db.AlignmentIncidents
        .Include(a => a.Plate)
        .OrderByDescending(a => a.CreatedAt)
        .Select(a => new IncidentDto(
            a.Id,
            a.PlateId,
            a.Plate != null ? a.Plate.SteelPlateNumber : string.Empty,
            a.StartImpressionId,
            a.EndImpressionId,
            a.Axis,
            a.Notes,
            a.IsResolved,
            a.CreatedAt
        ))
        .ToListAsync(ct);
    return Results.Ok(incidents);
})
.WithName("GetIncidents")
.WithOpenApi();

app.MapPut("/api/incidents/{id}/resolve", async (int id, ResolveIncidentDto dto, AppDbContext db, CancellationToken ct) =>
{
    var incident = await db.AlignmentIncidents.FirstOrDefaultAsync(a => a.Id == id, ct);
    if (incident == null)
        return Results.NotFound($"Incident with id {id} not found");

    incident.IsResolved = true;
    incident.ResolvedAt = DateTime.UtcNow;
    if (!string.IsNullOrWhiteSpace(dto.Notes))
        incident.Notes = dto.Notes;

    await db.SaveChangesAsync(ct);
    return Results.Ok(incident);
})
.WithName("ResolveIncident")
.WithOpenApi();

app.MapGet("/api/warnings", async (AppDbContext db, CancellationToken ct) =>
{
    var warnings = await db.Warnings
        .OrderByDescending(w => w.CreatedAt)
        .Select(w => new WarningDto(
            w.Id,
            w.PlateId,
            w.WarningType,
            w.Message,
            w.IsAcknowledged,
            w.CreatedAt
        ))
        .ToListAsync(ct);
    return Results.Ok(warnings);
})
.WithName("GetWarnings")
.WithOpenApi();

app.MapPut("/api/warnings/{id}/acknowledge", async (int id, AppDbContext db, CancellationToken ct) =>
{
    var warning = await db.Warnings.FirstOrDefaultAsync(w => w.Id == id, ct);
    if (warning == null)
        return Results.NotFound($"Warning with id {id} not found");

    warning.IsAcknowledged = true;
    warning.AcknowledgedAt = DateTime.UtcNow;

    await db.SaveChangesAsync(ct);
    return Results.Ok(warning);
})
.WithName("AcknowledgeWarning")
.WithOpenApi();

app.Run();
