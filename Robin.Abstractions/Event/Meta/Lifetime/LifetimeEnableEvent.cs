namespace Robin.Abstractions.Event.Meta.Lifetime;

public record LifetimeEnableEvent(long Time) : LifetimeEvent(Time);