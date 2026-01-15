using System.Collections.Frozen;
using System.Reflection;
using CronExpressionDescriptor;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Middlewares.Fluent.Cron;
using Robin.Middlewares.Fluent.Event;

namespace Robin.Middlewares.Fluent;

#region Event
[BotFunctionInfo("fluent", "元功能，流式扩展接口", typeof(BotEvent))]
public partial class FluentFunction(FunctionContext<FluentOption> context)
    : BotFunction<FluentOption>(context)
{
    private IEnumerable<IEnumerable<EventTunnel>> _eventTunLists = [];
    private IEnumerable<EventTunnel> _intrinsicEventTuns = [];

    private void AddEventTunnels(List<(IFluentFunction, FunctionInfo)> pairs)
    {
        var tunLists = new SortedList<int, List<EventTunnel>>();
        var intrinsicTuns = new List<EventTunnel>();

        pairs.ForEach(pair =>
        {
            var (function, info) = pair;
            var tunnels = info.EventTunnels.ToList();

            ((BotFunction)function).TriggerDescriptions.AddRange(tunnels.GetDescriptions());

            tunnels.ForEach(tunnel =>
            {
                if (tunnel.Priority == int.MinValue)
                    intrinsicTuns.Add(tunnel);
                else if (!tunLists.TryGetValue(tunnel.Priority, out var list))
                    tunLists[tunnel.Priority] = [tunnel];
                else
                    list.Add(tunnel);
            });
        });

        _eventTunLists = tunLists.Values;
        _intrinsicEventTuns = intrinsicTuns;
    }

    public override async Task OnEventAsync(EventContext<BotEvent> eventContext)
    {
        var tasks = new List<Task>();
        tasks.AddRange(
            (await Task.WhenAll(_intrinsicEventTuns.Select(tun => tun.Tunnel(eventContext))))
                .Where(res => res.Accept)
                .Select(res => res.Data!)
        );

        foreach (var list in _eventTunLists)
        {
            var fired = (await Task.WhenAll(list.Select(tunnel => tunnel.Tunnel(eventContext))))
                .Where(res => res.Accept)
                .Select(res => res.Data!);

            if (fired.Any())
            {
                tasks.AddRange(fired);
                break;
            }
        }

        await Task.WhenAll(tasks);
    }
}
#endregion

#region Cron
public partial class FluentFunction
{
    // Like BackgroundService, see https://github.com/dotnet/runtime/blob/cf66826ff76e570d0ed79f33725bdc50e09dc332/src/libraries/Microsoft.Extensions.Hosting.Abstractions/src/BackgroundService.cs
    private CancellationTokenSource? _stoppingCts;
    private IScheduler? _scheduler;

    private async Task AddCronTunnelsAsync(
        List<(IFluentFunction, FunctionInfo)> pairs,
        CancellationToken token
    )
    {
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(token);

        _scheduler = await new StdSchedulerFactory(
            new()
            {
                [StdSchedulerFactory.PropertySchedulerInstanceName] =
                    _context.BotContext.Uin.ToString(),
            }
        ).GetScheduler(token);

        var tuples = pairs.SelectMany(pair =>
        {
            var (function, info) = pair;
            var funcName = function.GetType().GetCustomAttribute<BotFunctionInfoAttribute>()!.Name;
            return info.CronTunnels.Select(tunnel =>
                (FuncName: funcName, Function: function, Tunnel: tunnel)
            );
        });

        _scheduler.JobFactory = new KeyedCronJobFactory(
            tuples.ToFrozenDictionary(t => (t.FuncName, t.Tunnel.Name!), t => t.Tunnel.Tunnel),
            _stoppingCts.Token
        );

        var options = new Options()
        {
            Use24HourTimeFormat = true,
            Locale = _context.Configuration.CronDescriptionLocale,
        };

        foreach (var (funcName, function, tunnel) in tuples)
        {
            if (
                !_context.Configuration.Crons.TryGetValue(funcName, out var funcCrons)
                || !funcCrons.TryGetValue(tunnel.Name!, out string? cron)
            )
                cron = tunnel.Cron;

            {
                var descTunnel = tunnel with
                {
                    Descriptions = tunnel.Descriptions.Prepend(
                        ExpressionDescriptor.GetDescription(cron, options) + " 自动触发"
                    ),
                };

                ((BotFunction)function).TriggerDescriptions.Add(descTunnel.GetDescription());
            }

            {
                var job = JobBuilder.Create<CronJob>().WithIdentity(tunnel.Name!, funcName).Build();

                var trigger = TriggerBuilder
                    .Create()
                    .WithIdentity(tunnel.Name!, funcName)
                    .WithCronSchedule(cron)
                    .Build();

                await _scheduler.ScheduleJob(job, trigger, token);
                LogCronJobScheduled(_context.Logger, funcName, tunnel.Name!, cron);
            }
        }

        await _scheduler.Start(token);
    }

    private async Task StopCronTunnelsAsync(CancellationToken token)
    {
        try
        {
            _stoppingCts!.Cancel();
        }
        finally
        {
            await _scheduler!
                .Shutdown(token)
                .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Cron job {Group}:{Name} scheduled at {Cron}"
    )]
    private static partial void LogCronJobScheduled(
        ILogger logger,
        string group,
        string name,
        string cron
    );
}
#endregion

public partial class FluentFunction
{
    public override async Task StartAsync(CancellationToken token)
    {
        Quartz.Logging.LogProvider.SetCurrentLogProvider(new CronLogProvider(_context.Logger));
        var pairs = (
            await Task.WhenAll(
                _context
                    .BotContext.Functions.OfType<IFluentFunction>()
                    .Select(async function =>
                    {
                        var functionBuilder = new FunctionBuilder((BotFunction)function);
                        await function.OnCreatingAsync(functionBuilder, token);
                        return (function, functionBuilder.Build());
                    })
            )
        ).ToList();

        AddEventTunnels(pairs);
        await AddCronTunnelsAsync(pairs, token);
    }

    public override async Task StopAsync(CancellationToken token)
    {
        await StopCronTunnelsAsync(token);
    }
}
