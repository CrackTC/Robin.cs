namespace Robin.Abstractions.Message.Entity;

public record CustomMusicData(string Url, string AudioUrl, string Title, string? Description, string? ImageUrl) : SegmentData;
