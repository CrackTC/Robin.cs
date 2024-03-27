namespace Robin.Abstractions.Event.Meta.Lifetime;

public record LifetimeConnectEvent(long Time) : LifetimeEvent(Time);