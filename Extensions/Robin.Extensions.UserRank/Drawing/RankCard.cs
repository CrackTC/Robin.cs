using SkiaSharp;
using Sorac.Skia.Canvas;
using Sorac.Skia.Color;
using Sorac.Skia.Font;
using Sorac.Skia.Rect;

namespace Robin.Extensions.UserRank.Drawing;

internal class RankCard(
    int rank,
    SKImage avatar,
    string name,
    float ratio,
    uint count,
    int delta,
    ColorPalette palette,
    float primaryFontSize
)
{
    private void DrawRank(
        SKCanvas canvas,
        FontMeasurement measurement,
        SKRect region
    )
    {
        var size = Math.Min(measurement.GetFitFontSize($"#{rank}", region.Size, out var parts), primaryFontSize);
        using var paint = new SKPaint { Color = palette.ForegroundSecondary, IsAntialias = true };
        canvas.DrawShapedCenteredText(parts, size, region, SKTextAlign.Left, paint);
    }

    private static readonly SKSamplingOptions _samplingOptions = new(SKFilterMode.Linear, SKMipmapMode.Linear);
    private void DrawAvatar(
        SKCanvas canvas,
        SKRect region
    )
    {
        canvas.Save();

        using var path = new SKPath();
        path.AddCircle(region.MidX, region.MidY, Math.Min(region.Width, region.Height) / 2);
        canvas.ClipPath(path, antialias: true);
        canvas.DrawImage(avatar, region.AspectFit(avatar.Info.Size), _samplingOptions);

        canvas.Restore();
    }

    private void DrawName(
        SKCanvas canvas,
        FontMeasurement measurement,
        SKRect region
    )
    {
        var size = Math.Min(measurement.GetFitFontSize(name, region.Size, out var parts), primaryFontSize * 0.8f);
        using var paint = new SKPaint { Color = palette.ForegroundSecondary, IsAntialias = true };
        canvas.DrawShapedCenteredText(parts, size, region, SKTextAlign.Left, paint);
    }

    private void DrawRatio(
        SKCanvas canvas,
        SKRect region
    )
    {
        var fullrect = region.Pad(0, region.Height / 3);
        var rect = fullrect with { Size = new SKSize(region.Width * ratio, fullrect.Height) };
        canvas.DrawRoundRect(fullrect, 10, 10, new SKPaint { Color = palette.BackgroundTertiary, Style = SKPaintStyle.Fill, IsAntialias = true });
        canvas.DrawRoundRect(rect, 10, 10, new SKPaint { Color = palette.Accent, Style = SKPaintStyle.Fill, IsAntialias = true });
    }

    private void DrawCount(
        SKCanvas canvas,
        FontMeasurement measurement,
        SKRect region
    )
    {
        var size = Math.Min(measurement.GetFitFontSize(count.ToString(), region.Size, out var parts), primaryFontSize * 0.8f);
        using var paint = new SKPaint { Color = palette.ForegroundSecondary, IsAntialias = true };
        canvas.DrawShapedCenteredText(parts, size, region, SKTextAlign.Right, paint);
    }

    private void DrawDelta(
        SKCanvas canvas,
        FontMeasurement measurement,
        SKRect region
    )
    {
        var text = delta switch
        {
            > 0 => $"▲{delta}",
            < 0 => $"▼{-delta}",
            _ => "-"
        };

        var size = Math.Min(measurement.GetFitFontSize(text, region.Size, out var parts), primaryFontSize * 0.8f);

        using var paint = new SKPaint
        {
            Color = delta switch { > 0 => palette.Success, < 0 => palette.Failure, _ => palette.ForegroundSecondary },
            IsAntialias = true
        };

        canvas.DrawShapedCenteredText(parts, size, region, SKTextAlign.Right, paint);
    }

    public void Draw(SKCanvas canvas, FontMeasurement measurement, SKRect region)
    {
        canvas.DrawRoundRect(region, 10, 10, new SKPaint { Color = palette.BackgroundSecondary, Style = SKPaintStyle.Fill, IsAntialias = true });
        DrawRank(canvas, measurement, SKRect.Create(region.Location, new SKSize(region.Width * 0.125f, region.Height)).Pad(20));
        DrawAvatar(canvas, SKRect.Create(region.Location + new SKPoint(region.Width * 0.125f, 0), new SKSize(region.Width * 0.125f, region.Height)));
        DrawName(canvas, measurement, SKRect.Create(region.Location + new SKPoint(region.Width * 0.25f, 0), new SKSize(region.Width * 0.375f, region.Height * 0.5f)).Pad(10, 20, 0, 0));
        DrawRatio(canvas, SKRect.Create(region.Location + new SKPoint(region.Width * 0.25f, region.Height * 0.5f), new SKSize(region.Width * 0.625f, region.Height * 0.5f)).Pad(10, 0, 5, 5));
        DrawCount(canvas, measurement, SKRect.Create(region.Location + new SKPoint(region.Width * 0.625f, 0), new SKSize(region.Width * 0.25f, region.Height * 0.5f)).Pad(10, 20, 0, 0));
        DrawDelta(canvas, measurement, SKRect.Create(region.Location + new SKPoint(region.Width * 0.875f, 0), new SKSize(region.Width * 0.125f, region.Height)).Pad(20));
    }
}
