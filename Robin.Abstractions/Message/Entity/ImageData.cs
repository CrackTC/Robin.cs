namespace Robin.Abstractions.Message.Entity;

public record ImageData(
    string File,
    string? Type = null,
    string? Url = null,
    string? Summary = null,
    bool? UseCache = null,
    bool? UseProxy = null,
    double? Timeout = null) : SegmentData;
