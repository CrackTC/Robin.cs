namespace Robin.Abstractions.Message.Entity;

public record ImageData(
    string File,
    ImageSubType Type = ImageSubType.Normal,
    string? Url = null,
    string? Summary = null,
    bool? UseCache = null,
    bool? UseProxy = null,
    double? Timeout = null
) : SegmentData;

public enum ImageSubType
{
    Normal,
    Sticker
}