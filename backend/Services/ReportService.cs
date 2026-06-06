using Microsoft.EntityFrameworkCore;
using PlateTracking.Data;
using PlateTracking.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PlateTracking.Services;

public class ReportService(AppDbContext db) : IReportService
{
    public async Task<byte[]> GeneratePlateReportAsync(int plateId, CancellationToken ct)
    {
        var plate = await db.Plates
            .Include(p => p.Impressions.OrderByDescending(i => i.CreatedAt).Take(100))
            .Include(p => p.Warnings.OrderByDescending(w => w.CreatedAt))
            .Include(p => p.AlignmentIncidents.OrderByDescending(a => a.CreatedAt))
            .FirstOrDefaultAsync(p => p.Id == plateId, ct);

        if (plate == null)
            throw new KeyNotFoundException($"Plate with id {plateId} not found");

        var data = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Calibri));

                page.Header()
                    .Column(column =>
                    {
                        column.Item()
                            .Text("烫金版材追溯报告")
                            .FontSize(20)
                            .Bold()
                            .AlignCenter();

                        column.Item()
                            .Text($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                            .FontSize(9)
                            .AlignRight()
                            .Gray();
                    });

                page.Content()
                    .PaddingVertical(10)
                    .Column(column =>
                    {
                        column.Item().Element(c => RenderPlateInfo(c, plate));
                        column.Item().PaddingVertical(10).Element(c => RenderLifeStatus(c, plate));
                        column.Item().PaddingVertical(10).Element(c => RenderImpressions(c, plate));
                        column.Item().PaddingVertical(10).Element(c => RenderWarnings(c, plate));
                        column.Item().PaddingVertical(10).Element(c => RenderIncidents(c, plate));
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("第 ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                        x.Span(" 页");
                    });
            });
        }).GeneratePdf();

        return data;
    }

    private void RenderPlateInfo(IContainer container, Plate plate)
    {
        container.Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(10)
            .Column(column =>
            {
                column.Item().Text("基本信息").Bold().FontSize(14).Underline();
                column.Item().PaddingTop(5).Grid(grid =>
                {
                    grid.Columns(2);
                    grid.Item().Text("钢版编号:").Bold();
                    grid.Item().Text(plate.SteelPlateNumber);
                    grid.Item().Text("设计凹深:").Bold();
                    grid.Item().Text($"{plate.DesignDepth} μm");
                    grid.Item().Text("寿命极限:").Bold();
                    grid.Item().Text($"{plate.LifeLimit} 次");
                    grid.Item().Text("当前压印次数:").Bold();
                    grid.Item().Text($"{plate.ImpressionCount} 次");
                    grid.Item().Text("状态:").Bold();
                    grid.Item().Text(plate.IsLocked ? "已锁定" : "正常").FontColor(plate.IsLocked ? Colors.Red.Medium : Colors.Green.Medium);
                    grid.Item().Text("登记时间:").Bold();
                    grid.Item().Text(plate.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                });
            });
    }

    private void RenderLifeStatus(IContainer container, Plate plate)
    {
        var percentage = (double)plate.ImpressionCount / plate.LifeLimit;
        var percentageText = $"{percentage:P0}";
        var color = percentage >= 1.0d ? Colors.Red.Medium
                   : percentage >= 0.8d ? Colors.Orange.Medium
                   : Colors.Green.Medium;

        container.Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(10)
            .Column(column =>
            {
                column.Item().Text("寿命状态").Bold().FontSize(14).Underline();
                column.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem()
                        .AlignMiddle()
                        .PaddingRight(10)
                        .Column(col =>
                        {
                            col.Item().Text($"进度: {plate.ImpressionCount}/{plate.LifeLimit}").Bold();
                            col.Item().PaddingTop(5)
                                .LinearProgress(progress =>
                                {
                                    progress.Ratio((float)Math.Min(percentage, 1.0));
                                    progress.Color(color);
                                    progress.Height(15);
                                });
                        });
                    row.ConstantItem(60)
                        .AlignCenter()
                        .AlignMiddle()
                        .Text(percentageText)
                        .FontSize(16)
                        .Bold()
                        .FontColor(color);
                });
            });
    }

    private void RenderImpressions(IContainer container, Plate plate)
    {
        container.Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(10)
            .Column(column =>
            {
                column.Item().Text($"压印记录 (最近 {plate.Impressions.Count} 条)").Bold().FontSize(14).Underline();
                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(40);
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("ID").Bold();
                        header.Cell().Text("X轴偏移(μm)").Bold();
                        header.Cell().Text("Y轴偏移(μm)").Bold();
                        header.Cell().Text("温度(°C)").Bold();
                        header.Cell().Text("时间").Bold();
                    });

                    foreach (var imp in plate.Impressions)
                    {
                        var xColor = Math.Abs(imp.OffsetX) > 0.08m ? Colors.Red.Medium : Colors.Black;
                        var yColor = Math.Abs(imp.OffsetY) > 0.08m ? Colors.Red.Medium : Colors.Black;

                        table.Cell().Text(imp.Id.ToString());
                        table.Cell().Text($"{imp.OffsetX:F4}").FontColor(xColor);
                        table.Cell().Text($"{imp.OffsetY:F4}").FontColor(yColor);
                        table.Cell().Text($"{imp.ActualTemperature:F1}");
                        table.Cell().Text(imp.CreatedAt.ToString("MM-dd HH:mm"));
                    }
                });
            });
    }

    private void RenderWarnings(IContainer container, Plate plate)
    {
        if (!plate.Warnings.Any()) return;

        container.Border(1)
            .BorderColor(Colors.Orange.Lighten2)
            .Background(Colors.Orange.Lighten5)
            .Padding(10)
            .Column(column =>
            {
                column.Item().Text("警告记录").Bold().FontSize(14).Underline().FontColor(Colors.Orange.Darken2);
                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(5);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("类型").Bold();
                        header.Cell().Text("消息").Bold();
                        header.Cell().Text("状态").Bold();
                        header.Cell().Text("时间").Bold();
                    });

                    foreach (var w in plate.Warnings)
                    {
                        table.Cell().Text(w.WarningType);
                        table.Cell().Text(w.Message);
                        table.Cell().Text(w.IsAcknowledged ? "已确认" : "待确认").FontColor(w.IsAcknowledged ? Colors.Green.Medium : Colors.Orange.Medium);
                        table.Cell().Text(w.CreatedAt.ToString("MM-dd HH:mm"));
                    }
                });
            });
    }

    private void RenderIncidents(IContainer container, Plate plate)
    {
        if (!plate.AlignmentIncidents.Any()) return;

        container.Border(1)
            .BorderColor(Colors.Red.Lighten2)
            .Background(Colors.Red.Lighten5)
            .Padding(10)
            .Column(column =>
            {
                column.Item().Text("套准异常记录").Bold().FontSize(14).Underline().FontColor(Colors.Red.Darken2);
                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(1);
                        cols.RelativeColumn(1);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(4);
                        cols.RelativeColumn(1);
                        cols.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("ID").Bold();
                        header.Cell().Text("轴").Bold();
                        header.Cell().Text("压印范围").Bold();
                        header.Cell().Text("备注").Bold();
                        header.Cell().Text("状态").Bold();
                        header.Cell().Text("时间").Bold();
                    });

                    foreach (var inc in plate.AlignmentIncidents)
                    {
                        table.Cell().Text(inc.Id.ToString());
                        table.Cell().Text(inc.Axis);
                        table.Cell().Text($"{inc.StartImpressionId}-{inc.EndImpressionId}");
                        table.Cell().Text(inc.Notes ?? string.Empty);
                        table.Cell().Text(inc.IsResolved ? "已解决" : "待处理").FontColor(inc.IsResolved ? Colors.Green.Medium : Colors.Red.Medium);
                        table.Cell().Text(inc.CreatedAt.ToString("MM-dd HH:mm"));
                    }
                });
            });
    }
}
