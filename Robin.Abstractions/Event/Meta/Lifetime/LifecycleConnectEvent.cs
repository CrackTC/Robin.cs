namespace Robin.Abstractions.Event.Meta.Lifetime;

public record LifecycleConnectEvent(long Time) : LifetimeEvent(Time);