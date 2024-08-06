namespace Robin.Abstractions.Message.Entity;

public record VideoData(string File, string? Url, bool? UseCache, bool? UseProxy, double? Timeout) : SegmentData;