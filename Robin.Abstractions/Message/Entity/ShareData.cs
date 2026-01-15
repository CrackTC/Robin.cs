namespace Robin.Abstractions.Message.Entity;

public record ShareData(string Url, string Title, string? Description, string? ImageUrl)
    : SegmentData;
