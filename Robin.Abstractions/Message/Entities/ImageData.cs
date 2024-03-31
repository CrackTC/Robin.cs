namespace Robin.Abstractions.Message.Entities;

public record ImageData(
    string File,
    string? Type = null,
    string? Url = null,
    bool? UseCache = null,
    bool? UseProxy = null,
    double? Timeout = null) : SegmentData;