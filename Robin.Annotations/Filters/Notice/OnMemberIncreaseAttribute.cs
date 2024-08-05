using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Notice.Member.Increase;

namespace Robin.Annotations.Filters.Notice;

public class OnMemberIncreaseAttribute(int filterGroup = 0) : OnNoticeAttribute(filterGroup)
{
    public override bool FilterEvent(long selfId, BotEvent @event)
    {
        if (!base.FilterEvent(selfId, @event)) return false;
        return @event is GroupIncreaseEvent;
    }

    public override string GetDescription() => "有新成员加群";
}