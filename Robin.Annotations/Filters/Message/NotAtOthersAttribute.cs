using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entities;

namespace Robin.Annotations.Filters.Message;

public class NotAtOthersAttribute(int filterGroup = 0) : OnMessageAttribute(filterGroup)
{
    public override bool FilterEvent(long selfId, BotEvent @event)
    {
        if (!base.FilterEvent(selfId, @event)) return false;

        var e = @event as MessageEvent;
        return e!.Message.All(segment => segment is not AtData at || at.Uin == selfId);
    }

    public override string GetDescription() => $"not_at_others, {base.GetDescription()}";
}