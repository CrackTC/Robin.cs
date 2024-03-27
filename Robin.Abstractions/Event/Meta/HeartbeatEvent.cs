using Robin.Abstractions.Operation.Entities;

namespace Robin.Abstractions.Event.Meta;

public record HeartbeatEvent(
    long Time,
    BotStatus Status,
    long Interval
) : MetaEvent(Time);