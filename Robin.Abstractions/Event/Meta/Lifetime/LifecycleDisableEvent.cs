namespace Robin.Abstractions.Event.Meta.Lifetime;

public record LifecycleDisableEvent(long Time) : LifetimeEvent(Time);