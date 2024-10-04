namespace Robin.Abstractions.Event.Meta.Lifetime;

[EventDescription("上游连接")]
public record LifecycleConnectEvent(long Time) : LifetimeEvent(Time);
