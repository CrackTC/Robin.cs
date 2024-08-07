using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Notice;

namespace Robin.Annotations.Filters.Notice;

public class OnPokeSelfAttribute(int filterGroup = 0) : BaseEventFilterAttribute(filterGroup)
{
    public override bool FilterEvent(EventContext eventContext) =>
        eventContext.Event is GroupPokeEvent e && e.TargetId == eventContext.Uin;

    public override string GetDescription() => "自身在群聊中被戳一戳";
}