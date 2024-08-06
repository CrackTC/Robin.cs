namespace Robin.Abstractions.Entity;

public record UploadFile(
    string Id,
    string Name,
    long Size,
    long BusId
);