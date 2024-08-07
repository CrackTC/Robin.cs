using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Fluent.Builder;

namespace Robin.Fluent;

[BotFunctionInfo("fluent", "元功能，流式扩展接口", typeof(BotEvent))]
public class FluentFunction(FunctionContext context) : BotFunction(context)
{
    private IEnumerable<IEnumerable<TunnelInfo>> _tunnelLists = [];

    public override Task StartAsync(CancellationToken token)
    {
        var tunnelLists = new SortedList<int, List<TunnelInfo>>();
        foreach (var function in _context.Functions.OfType<IFluentFunction>())
        {
            var functionBuilder = new FunctionBuilder();

            function.OnCreating(functionBuilder);

            var infos = functionBuilder.Build();

            function.Descriptions = infos.Select(info => string.Join(' ', info.Descriptions)).ToArray();

            foreach (var info in infos)
            {
                if (!tunnelLists.TryGetValue(info.Priority, out List<TunnelInfo>? list))
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
        return Task.CompletedTask;
    }

    public override Task OnEventAsync(EventContext<BotEvent> eventContext)
    {
        foreach (var list in _tunnelLists)
        {
            var tasks = list
                .Select(tunnel => tunnel.Tunnel(eventContext))
                .Where(res => res.Accept)
                .Select(res => res.Data!)
                .ToList();

            if (tasks.Count is not 0)
            {
                return Task.WhenAll(tasks);
            }
        }

        return Task.CompletedTask;
    }
}
