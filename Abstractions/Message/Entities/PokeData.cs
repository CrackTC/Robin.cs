namespace Robin.Abstractions.Message.Entities;

public record PokeData(int Type, int Id, string? Name) : SegmentData;