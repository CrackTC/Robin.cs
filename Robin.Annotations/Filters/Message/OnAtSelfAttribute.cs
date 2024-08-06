using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;

namespace Robin.Annotations.Filters.Message;

public class OnAtSelfAttribute(int filterGroup = 0) : OnMessageAttribute(filterGroup)
{
    public override bool FilterEvent(long selfId, BotEvent @event)
    {
        if (!base.FilterEvent(selfId, @event)) return false;

        var e = @event as MessageEvent;
        return e!.Message.Any(segment => segment is AtData at && at.Uin == selfId);
    }

    public override string GetDescription() => "自身在群聊中被@";
}