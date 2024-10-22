using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Fluent.Event;

namespace Robin.Fluent;

[BotFunctionInfo("fluent", "元功能，流式扩展接口", typeof(BotEvent))]
// ReSharper disable once UnusedType.Global
public class FluentFunction(FunctionContext context) : BotFunction(context)
{
    private IEnumerable<IEnumerable<EventTunnelInfo>> _eventTunLists = [];
    private IEnumerable<EventTunnelInfo> _alwaysFiredEventTuns = [];

    public override async Task StartAsync(CancellationToken token)
    {
        var eventTunLists = new SortedList<int, List<EventTunnelInfo>>();
        var alwaysFiredEventTuns = new List<EventTunnelInfo>();

        foreach (var function in _context.Functions.OfType<IFluentFunction>())
        {
            var functionBuilder = new FunctionBuilder();

            await function.OnCreatingAsync(functionBuilder, token);

            var infos = functionBuilder.Build().ToList();

            (function as BotFunction)?.TriggerDescriptions.AddRange(infos.Select(info => string.Join(" 且 ", info.Descriptions)));

            foreach (var info in infos)
            {
                if (info.Priority == int.MinValue)
                {
                    alwaysFiredEventTuns.Add(info);
                }
                else if (!eventTunLists.TryGetValue(info.Priority, out var list))
                {
                    eventTunLists[info.Priority] = [info];
                }
                else
                {
                    list.Add(info);
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
