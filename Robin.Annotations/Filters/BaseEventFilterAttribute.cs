using Robin.Abstractions.Context;
using Robin.Abstractions.Event;

namespace Robin.Annotations.Filters;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public abstract class BaseEventFilterAttribute(int filterGroup) : TriggerAttribute
{
    public int FilterGroup { get; } = filterGroup;

    public abstract bool FilterEvent(EventContext<BotEvent> eventContext);
}