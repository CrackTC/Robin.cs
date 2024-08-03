using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Notice;

namespace Robin.Annotations.Filters.Notice;

public abstract class OnNoticeAttribute(int filterGroup = 0) : BaseEventFilterAttribute(filterGroup)
{
    public override bool FilterEvent(long selfId, BotEvent @event) => @event is NoticeEvent;
}