using Robin.Abstractions.Event;

namespace Robin.Abstractions.Context;

public class EventContext<TEvent>(
    long uin,
    TEvent @event,
    CancellationToken token
) where TEvent : BotEvent
{
    public long Uin => uin;
    public TEvent Event => @event;
    public CancellationToken Token => token;
}