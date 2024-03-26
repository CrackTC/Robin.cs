namespace Robin.Abstractions.Event.Notice.File;

public record UploadFile(
    string Id,
    string Name,
    long Size,
    long BusId
);