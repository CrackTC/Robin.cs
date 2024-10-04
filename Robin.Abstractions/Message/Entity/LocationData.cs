namespace Robin.Abstractions.Message.Entity;

public record LocationData(double Latitude, double Longitude, string? Title, string? Description) : SegmentData;
