using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Notice;

namespace Robin.Annotations.Filters.Notice;

public class OnPokeSelfAttribute(int filterGroup = 0) : BaseEventFilterAttribute(filterGroup)
{
    public override bool FilterEvent(long selfId, BotEvent @event) =>
        @event is GroupPokeEvent e && e.TargetId == selfId;

    public override string GetDescription() => "自身在群聊中被戳一戳";
}