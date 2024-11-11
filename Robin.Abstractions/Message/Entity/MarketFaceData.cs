namespace Robin.Abstractions.Message.Entity;

public record MarketFaceData(
    string Url,
    string EmojiId,
    int EmojiPackageId,
    string Key,
    string? Summary
) : SegmentData;
