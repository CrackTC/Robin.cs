namespace Robin.Abstractions.Message.Entities;

public record CustomMusicData(string Url, string AudioUrl, string Title, string? Description, string? ImageUrl) : SegmentData;