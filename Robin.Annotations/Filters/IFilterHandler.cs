using Robin.Abstractions.Event;

namespace Robin.Annotations.Filters;

public interface IFilterHandler
{
    Task OnFilteredEventAsync(int filterGroup, long selfId, BotEvent @event, CancellationToken token);
}