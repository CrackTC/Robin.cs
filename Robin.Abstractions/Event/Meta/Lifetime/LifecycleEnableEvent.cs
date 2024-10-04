namespace Robin.Abstractions.Event.Meta.Lifetime;

[EventDescription("上游启用")]
public record LifecycleEnableEvent(long Time) : LifetimeEvent(Time);
