namespace Robin.Abstractions.Event.Meta.Lifetime;

public record LifecycleEnableEvent(long Time) : LifetimeEvent(Time);