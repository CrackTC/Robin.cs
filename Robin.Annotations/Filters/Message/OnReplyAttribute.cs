using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entities;

namespace Robin.Annotations.Filters.Message;

public class OnReplyAttribute(int filterGroup = 0) : OnMessageAttribute(filterGroup)
{
    public override bool FilterEvent(long selfId, BotEvent @event)
    {
        if (!base.FilterEvent(selfId, @event)) return false;

        var e = @event as MessageEvent;
        return e!.Message.Any(segment => segment is ReplyData);
    }

    public override string GetDescription() => $"on_reply, {base.GetDescription()}";
}