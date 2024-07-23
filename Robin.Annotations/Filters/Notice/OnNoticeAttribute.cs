using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Notice;

namespace Robin.Annotations.Filters.Notice;

public class OnNoticeAttribute(int filterGroup = 0) : BaseEventFilterAttribute(filterGroup)
{
    public override bool FilterEvent(long selfId, BotEvent @event) => @event is NoticeEvent;

    public override string GetDescription() => $"on_notice, {base.GetDescription()}";
}