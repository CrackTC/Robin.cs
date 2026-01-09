namespace Robin.Abstractions.Message.Entity;

public record VideoData(
    string File,
    string? Url = null,
    bool? UseCache = null,
    bool? UseProxy = null,
    double? Timeout = null
) : SegmentData;