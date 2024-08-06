using System.Collections.Frozen;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Robin.Abstractions;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Event;

namespace Robin.Annotations.Filters;

[BotFunctionInfo("filter", "元功能，面向方面订阅和分派事件", typeof(BotEvent))]
public class EventFilterFunction(
    IServiceProvider service,
    long uin,
    IOperationProvider provider,
    IConfiguration configuration,
    IEnumerable<BotFunction> functions
) : BotFunction(service, uin, provider, configuration, functions)
{
    private FrozenSet<(FrozenSet<FrozenSet<BaseEventFilterAttribute>> FilterGroups, IFilterHandler Handler)>? _nonFallbackHandlers;
    private FrozenSet<(FrozenSet<FrozenSet<BaseEventFilterAttribute>> FilterGroups, IFilterHandler Handler)>? _fallbackHandlers;
    public override async Task OnEventAsync(long selfId, BotEvent @event, CancellationToken token)
    {
        var tasks = new List<Task<bool>>();
        foreach (var (filterGroups, handler) in _nonFallbackHandlers!)
        {
            if (filterGroups.FirstOrDefault(filterGroup =>
                    filterGroup.All(filter => filter.FilterEvent(selfId, @event))) is { } group)
            {
                tasks.Add(handler.OnFilteredEventAsync(group.First().FilterGroup, selfId, @event, token));
            }
        }

        if ((await Task.WhenAll(tasks)).Any(result => result)) return;

        foreach (var (filterGroups, handler) in _fallbackHandlers!)
        {
            if (filterGroups.FirstOrDefault(filterGroup =>
                    filterGroup.All(filter => filter.FilterEvent(selfId, @event))) is not
                    { } group) continue;
            tasks.Add(handler.OnFilteredEventAsync(group.First().FilterGroup, selfId, @event, token));
            break;
        }

        await Task.WhenAll(tasks);
    }
    public override Task StartAsync(CancellationToken token)
    {
        var attributes = _functions
            .OfType<IFilterHandler>()
            .Select(handler => (Handler: handler, Attributes: handler.GetType().GetCustomAttributes<BaseEventFilterAttribute>()))
            .Where(pair => pair.Attributes.Any())
            .ToList();

        _nonFallbackHandlers = attributes
            .Where(pair => pair.Attributes.All(attr => attr is not FallbackAttribute))
            .Select(pair => (
                pair.Attributes
                    .GroupBy(attr => attr.FilterGroup)
                    .Select(group => group.ToFrozenSet())
                    .ToFrozenSet(),
                pair.Handler))
            .ToFrozenSet();

        _fallbackHandlers = attributes
            .Where(pair => pair.Attributes.Any(attr => attr is FallbackAttribute))
            .Select(pair => (
                pair.Attributes
                    .GroupBy(attr => attr.FilterGroup)
                    .Select(group => group.ToFrozenSet())
                    .ToFrozenSet(),
                pair.Handler))
            .ToFrozenSet();

        return Task.CompletedTask;
    }
    public override Task StopAsync(CancellationToken token) => Task.CompletedTask;
}