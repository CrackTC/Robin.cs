using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Middlewares.Fluent.Event;

namespace Robin.Middlewares.Fluent;

[BotFunctionInfo("fluent", "元功能，流式扩展接口", typeof(BotEvent))]
public class FluentFunction(FunctionContext context) : BotFunction(context)
{
    private IEnumerable<IEnumerable<EventTunnel>> _eventTunLists = [];
    private IEnumerable<EventTunnel> _alwaysFiredEventTuns = [];

    public override async Task StartAsync(CancellationToken token)
    {
        var eventTunLists = new SortedList<int, List<EventTunnel>>();
        var alwaysFiredEventTuns = new List<EventTunnel>();

        foreach (var function in _context.BotContext.Functions.OfType<IFluentFunction>())
        {
            var functionBuilder = new FunctionBuilder();

            await function.OnCreatingAsync(functionBuilder, token);

            var info = functionBuilder.Build();
            var eventTunnels = info.EventTunnels.ToList();

            (function as BotFunction)?.TriggerDescriptions.AddRange(eventTunnels.Select(info => string.Join(" 且 ", info.Descriptions)));

            foreach (var eventTunnel in eventTunnels)
            {
                if (eventTunnel.Priority == int.MinValue)
                {
                    alwaysFiredEventTuns.Add(eventTunnel);
                }
                else if (!eventTunLists.TryGetValue(eventTunnel.Priority, out var list))
                {
                    eventTunLists[eventTunnel.Priority] = [eventTunnel];
                }
                else
                {
                    list.Add(eventTunnel);
                }
            }
        }

        _eventTunLists = eventTunLists.Values;
        _alwaysFiredEventTuns = alwaysFiredEventTuns;
    }

    public override Task OnEventAsync(EventContext<BotEvent> eventContext)
    {
        var tasks = new List<Task>();

        tasks.AddRange(_alwaysFiredEventTuns
            .Select(tunnel => tunnel.Tunnel(eventContext))
            .Where(res => res.Accept)
            .Select(res => res.Data!));

        foreach (var list in _eventTunLists)
        {
            var fired = list
                .Select(tunnel => tunnel.Tunnel(eventContext))
                .Where(res => res.Accept)
                .Select(res => res.Data!)
                .ToList();

            if (fired.Count is not 0)
            {
                tasks.AddRange(fired);
                break;
            }
        }

        return Task.WhenAll(tasks);
    }
}
