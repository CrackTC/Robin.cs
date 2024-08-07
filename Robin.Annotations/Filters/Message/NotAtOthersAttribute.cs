using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;

namespace Robin.Annotations.Filters.Message;

// ReSharper disable once UnusedType.Global
public class NotAtOthersAttribute(int filterGroup = 0) : BaseEventFilterAttribute(filterGroup)
{
    public override bool FilterEvent(EventContext<BotEvent> eventContext) =>
        eventContext.Event is MessageEvent e
        && e.Message.All(segment => segment is not AtData at
                                    || at.Uin == eventContext.Uin);

    public override string GetDescription() => "消息未@其他人";
}