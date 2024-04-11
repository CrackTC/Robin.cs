using Robin.Abstractions.Event;

namespace Robin.Annotations.Filters;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public abstract class BaseEventFilterAttribute(int filterGroup) : Attribute
{
    public int FilterGroup { get; } = filterGroup;

    public abstract bool FilterEvent(long selfId, BotEvent @event);
}