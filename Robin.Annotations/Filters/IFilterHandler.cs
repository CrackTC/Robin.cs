using Robin.Abstractions.Event;

namespace Robin.Annotations.Filters;

public interface IFilterHandler
{
    Task<bool> OnFilteredEventAsync(int filterGroup, long selfId, BotEvent @event, CancellationToken token);
}