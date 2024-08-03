namespace Robin.Abstractions.Event.Meta.Lifetime;

[EventDescription("上游禁用")]
public record LifecycleDisableEvent(long Time) : LifetimeEvent(Time);