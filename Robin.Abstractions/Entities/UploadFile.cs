namespace Robin.Abstractions.Entities;

public record UploadFile(
    string Id,
    string Name,
    long Size,
    long BusId
);