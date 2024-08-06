using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;

namespace Robin.Annotations.Filters.Message;

public class NotAtOthersAttribute(int filterGroup = 0) : OnMessageAttribute(filterGroup)
{
    public override bool FilterEvent(long selfId, BotEvent @event)
    {
        if (!base.FilterEvent(selfId, @event)) return false;

        var e = @event as MessageEvent;
        return e!.Message.All(segment => segment is not AtData at || at.Uin == selfId);
    }

    public override string GetDescription() => "消息未@其他人";
}