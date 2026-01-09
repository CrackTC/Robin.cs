namespace Robin.Abstractions.Message.Entity;

public record RecordData(
    string File,
    bool? IsMagic = null,
    string? Url = null,
    bool? UseCache = null,
    bool? UseProxy = null,
    double? Timeout = null
) : SegmentData;
