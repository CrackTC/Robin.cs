using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;

namespace Robin.Annotations.Filters.Message;

public class OnReplyAttribute(int filterGroup = 0) : BaseEventFilterAttribute(filterGroup)
{
    public override bool FilterEvent(EventContext<BotEvent> eventContext) =>
        eventContext.Event is MessageEvent e
        && e.Message.Any(segment => segment is ReplyData);

    public override string GetDescription() => "消息包含对其它消息的回复";
}