using SkiaSharp;
using Sorac.Skia.Color;
using Sorac.Skia.Font;

namespace Robin.Extensions.UserRank.Drawing;

internal class RankImage(
    IEnumerable<string> fontPaths,
    Dictionary<string, string> paletteDict,
    float cardWidth = 720,
    float cardHeight = 120,
    float cardGap = 20,
    float primaryFontSize = 40
)
{
    private readonly FontMeasurement measurement = new(fontPaths.Select(fontPath => SKTypeface.FromFile(fontPath)).ToList());
    private readonly ColorPalette palette = ColorPalette.FromDictionary(paletteDict);

    private static readonly HttpClient _client = new();
    private static async Task<SKImage> FetchGroupAvatarAsync(long groupId, CancellationToken token)
    {
        using var stream = await _client.GetStreamAsync($"https://p.qlogo.cn/gh/{groupId}/{groupId}/640", token);
        return SKImage.FromEncodedData(stream);
    }
    private static async Task<SKImage> FetchUserAvatarAsync(long userId, CancellationToken token)
    {
        using var stream = await _client.GetStreamAsync($"https://q1.qlogo.cn/g?b=qq&nk={userId}&s=640", token);
        return SKImage.FromEncodedData(stream);
    }

    public async Task<SKImage> GenerateAsync(
        string groupName,
        long groupId,
        int people,
        uint count,
        List<(int rank, long userId, string name, uint count, int delta)> ranks,
        CancellationToken token
    )
    {
        float width = cardWidth + cardGap * 2;
        float height = cardHeight * (ranks.Count + 1) + cardGap * (ranks.Count + 2);
        var info = new SKImageInfo((int)width, (int)height);
        using var surface = SKSurface.Create(info);
        surface.Canvas.Clear(palette.Background);

        using var groupAvatar = await FetchGroupAvatarAsync(groupId, token);
        var rankHeader = new RankHeader(groupName, groupAvatar, people, count, palette, primaryFontSize);
        rankHeader.Draw(surface.Canvas, measurement, SKRect.Create(cardGap, cardGap, cardWidth, cardHeight));

        var userAvatars = await Task.WhenAll(ranks.Select(rank => FetchUserAvatarAsync(rank.userId, token)));

        uint? max = null;

        float y = cardHeight + cardGap * 2;
        for (int i = 0; i < ranks.Count; i++)
        {
            max ??= ranks.Max(rank => rank.count);
            var (userRank, userId, userName, userCount, userDelta) = ranks[i];
            using var userAvatar = userAvatars[i];
            var rankCard = new RankCard(userRank, userAvatar, userName, (float)userCount / max.Value, userCount, userDelta, palette, primaryFontSize);
            rankCard.Draw(surface.Canvas, measurement, SKRect.Create(cardGap, y, cardWidth, cardHeight));
            y += cardHeight + cardGap;
        }

        return surface.Snapshot();
    }
}
