using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;

namespace Robin.Annotations.Filters.Message;

public class OnPrivateMessageAttribute(int filterGroup = 0) : BaseEventFilterAttribute(filterGroup)
{
    public override bool FilterEvent(EventContext eventContext) => eventContext.Event is PrivateMessageEvent;

    public override string GetDescription() => "收到私聊消息";
}