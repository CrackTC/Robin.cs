using Robin.Abstractions.Event;

namespace Robin.Abstractions.Context;

public class EventContext(
    long uin,
    BotEvent @event,
    CancellationToken token
)
{
    public long Uin => uin;
    public BotEvent Event => @event;
    public CancellationToken Token => token;
}