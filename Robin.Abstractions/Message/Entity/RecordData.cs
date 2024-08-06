namespace Robin.Abstractions.Message.Entity;

public record RecordData(string File, bool IsMagic, string? Url, bool? UseCache, bool? UseProxy, double? Timeout) : SegmentData;