namespace Robin.Abstractions.Message.Entities;

public record ImageData(string File, string? Type, string? Url, bool? UseCache, bool? UseProxy, int? Timeout) : SegmentData;