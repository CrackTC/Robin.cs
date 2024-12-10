using SkiaSharp;
using Sorac.Skia.Canvas;
using Sorac.Skia.Color;
using Sorac.Skia.Font;
using Sorac.Skia.Rect;

namespace Robin.Extensions.UserRank.Drawing;

internal class RankHeader(
    string name,
    SKImage avatar,
    int people,
    uint count,
    ColorPalette palette,
    float primaryFontSize
)
{
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
        var size = Math.Min(measurement.GetFitFontSize(name, region.Size, out var widths), primaryFontSize);
        using var paint = new SKPaint { Color = palette.ForegroundTertiary, IsAntialias = true };
        canvas.DrawShapedCenteredText(widths, size, region, SKTextAlign.Left, paint);
    }

    private void DrawCount(
        SKCanvas canvas,
        FontMeasurement measurement,
        SKRect region
    )
    {
        var size = Math.Min(measurement.GetFitFontSize($"本群 {people} 位朋友共产生 {count} 条发言", region.Size, out var parts), primaryFontSize * 0.8f);
        using var paint = new SKPaint { Color = palette.ForegroundTertiary, IsAntialias = true };
        canvas.DrawShapedCenteredText(parts, size, region, SKTextAlign.Left, paint);
    }

    public void Draw(SKCanvas canvas, FontMeasurement measurement, SKRect region)
    {
        canvas.DrawRoundRect(region, 10, 10, new SKPaint { Color = palette.BackgroundTertiary, Style = SKPaintStyle.Fill, IsAntialias = true });
        DrawAvatar(canvas, SKRect.Create(region.Location, new SKSize(region.Height, region.Height)).Pad(10));
        DrawName(canvas, measurement, SKRect.Create(region.Location + new SKPoint(region.Height, 0), new SKSize(region.Width - region.Height, region.Height * 0.5f)).Pad(5, 20, 0, 0));
        DrawCount(canvas, measurement, SKRect.Create(region.Location + new SKPoint(region.Height, region.Height * 0.5f), new SKSize(region.Width - region.Height, region.Height * 0.5f)).Pad(5, 20, 10, 10));
    }
}
