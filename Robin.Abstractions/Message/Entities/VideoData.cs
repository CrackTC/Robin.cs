namespace Robin.Abstractions.Message.Entities;

public record VideoData(string File, string? Url, bool? UseCache, bool? UseProxy, double? Timeout) : SegmentData;