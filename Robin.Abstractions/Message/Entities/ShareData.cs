namespace Robin.Abstractions.Message.Entities;

public record ShareData(string Url, string Title, string? Description, string? ImageUrl) : SegmentData;