using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Notice;

namespace Robin.Annotations.Filters.Notice;

public class OnPokeSelfAttribute(int filterGroup = 0) : OnNoticeAttribute(filterGroup)
{
    public override bool FilterEvent(long selfId, BotEvent @event)
    {
        if (!base.FilterEvent(selfId, @event)) return false;
        return @event is GroupPokeEvent poke && poke.TargetId == selfId;
    }

    public override string GetDescription() => $"poke_self, {base.GetDescription()}";
}