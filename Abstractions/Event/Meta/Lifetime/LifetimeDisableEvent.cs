namespace Robin.Abstractions.Event.Meta.Lifetime;

public record LifetimeDisableEvent(long Time) : LifetimeEvent(Time);