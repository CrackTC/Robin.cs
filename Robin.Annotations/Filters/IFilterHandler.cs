using Robin.Abstractions.Context;
using Robin.Abstractions.Event;

namespace Robin.Annotations.Filters;

public interface IFilterHandler
{
    // ReSharper disable once UnusedParameter.Global
    Task<bool> OnFilteredEventAsync(int filterGroup, EventContext<BotEvent> eventContext);
}