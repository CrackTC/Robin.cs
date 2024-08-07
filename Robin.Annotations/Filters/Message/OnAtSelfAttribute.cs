using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;

namespace Robin.Annotations.Filters.Message;

public class OnAtSelfAttribute(int filterGroup = 0) : BaseEventFilterAttribute(filterGroup)
{
    public override bool FilterEvent(long selfId, BotEvent @event) =>
        @event is MessageEvent e
        && e.Message.Any(segment => segment is AtData at
                                    && at.Uin == selfId);

    public override string GetDescription() => "自身在群聊中被@";
}