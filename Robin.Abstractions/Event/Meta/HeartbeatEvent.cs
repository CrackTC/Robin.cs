using Robin.Abstractions.Entities;

namespace Robin.Abstractions.Event.Meta;

[EventDescription("心跳包")]
public record HeartbeatEvent(
    long Time,
    BotStatus Status,
    long Interval
) : MetaEvent(Time);