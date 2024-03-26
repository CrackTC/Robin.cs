namespace Robin.Abstractions.Message.Entities;

public record LocationData(double Latitude, double Longitude, string? Title, string? Description) : SegmentData;