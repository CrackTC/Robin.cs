using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Fluent.Builder;

namespace Robin.Fluent;

[BotFunctionInfo("fluent", "元功能，流式扩展接口", typeof(BotEvent))]
// ReSharper disable once UnusedType.Global
public class FluentFunction(FunctionContext context) : BotFunction(context)
{
    private IEnumerable<IEnumerable<TunnelInfo>> _tunnelLists = [];
    private IEnumerable<TunnelInfo> _alwaysFiredTunnels = [];

    public override async Task StartAsync(CancellationToken token)
    {
        var tunnelLists = new SortedList<int, List<TunnelInfo>>();
        var alwaysFiredTunnels = new List<TunnelInfo>();

        foreach (var function in _context.Functions.OfType<IFluentFunction>())
        {
            var functionBuilder = new FunctionBuilder();

            await function.OnCreatingAsync(functionBuilder, token);

            var infos = functionBuilder.Build().ToList();

            function.Description = string.Join('\n', infos.Select(info => "• " + string.Join(" 且 ", info.Descriptions)));

            foreach (var info in infos)
            {
                if (info.Priority == int.MinValue)
                {
                    alwaysFiredTunnels.Add(info);
                }
                else if (!tunnelLists.TryGetValue(info.Priority, out var list))
                {
                    tunnelLists[info.Priority] = [info];
                }
                else
                {
                    list.Add(info);
                }
            }
        }

        _tunnelLists = tunnelLists.Values;
        _alwaysFiredTunnels = alwaysFiredTunnels;
    }

    public override Task OnEventAsync(EventContext<BotEvent> eventContext)
    {
        var tasks = new List<Task>();

        tasks.AddRange(_alwaysFiredTunnels
            .Select(tunnel => tunnel.Tunnel(eventContext))
            .Where(res => res.Accept)
            .Select(res => res.Data!));

        foreach (var list in _tunnelLists)
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
