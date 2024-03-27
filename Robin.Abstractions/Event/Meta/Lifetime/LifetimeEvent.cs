namespace Robin.Abstractions.Event.Meta.Lifetime;

public record LifetimeEvent(long Time) : MetaEvent(Time);