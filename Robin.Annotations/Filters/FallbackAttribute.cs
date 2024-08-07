using Robin.Abstractions.Context;
using Robin.Abstractions.Event;

namespace Robin.Annotations.Filters;

public class FallbackAttribute() : BaseEventFilterAttribute(0)
{
    public override bool FilterEvent(EventContext<BotEvent> eventContext) => true;
    public override string GetDescription() => "未触发其它功能";
}