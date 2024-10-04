namespace Robin.Abstractions.Message.Entity;

public record PokeData(int Type, int Id, string? Name) : SegmentData;
