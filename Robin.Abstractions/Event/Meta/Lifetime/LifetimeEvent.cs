namespace Robin.Abstractions.Event.Meta.Lifetime;

[EventDescription("上游事件")]
public record LifetimeEvent(long Time) : MetaEvent(Time);
