using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;

namespace Robin.Annotations.Filters.Message;

public class OnPrivateMessageAttribute(int filterGroup = 0) : BaseEventFilterAttribute(filterGroup)
{
    public override bool FilterEvent(long selfId, BotEvent @event) => @event is PrivateMessageEvent;

    public override string GetDescription() => $"on_private_message, {base.GetDescription()}";
}