using System.Collections.Frozen;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using Robin.Abstractions;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Event;

namespace Robin.Annotations.Cron;

[BotFunctionInfo("cron", "trigger a function on a cron schedule")]
public partial class CronFunction(
    IServiceProvider service,
    long uin,
    IOperationProvider operation,
    IConfiguration configuration,
    IEnumerable<BotFunction> functions) : BotFunction(service, uin, operation, configuration, functions)
{
    private IScheduler? _scheduler;
    private readonly ILogger<CronFunction> _logger = service.GetRequiredService<ILogger<CronFunction>>();
    public override Task OnEventAsync(long selfId, BotEvent @event, CancellationToken token) => throw new InvalidOperationException();
    public override async Task StartAsync(CancellationToken token)
    {
        var handlers = _functions
            .OfType<ICronHandler>()
            .Select(handler => (
                Handler: handler,
                InfoAttribute: handler.GetType().GetCustomAttribute<BotFunctionInfoAttribute>(),
                CronAttribute: handler.GetType().GetCustomAttribute<OnCronAttribute>()))
            .Where(tuple => tuple.InfoAttribute is not null && tuple.CronAttribute is not null)
            .Select(tuple => (tuple.Handler, tuple.InfoAttribute!.Name, tuple.CronAttribute!.Cron))
            .ToList();

        _scheduler = await new StdSchedulerFactory().GetScheduler(token);
        _scheduler.JobFactory = new CronJobFactory(
            handlers.ToFrozenDictionary(tuple => tuple.Name, tuple => tuple.Handler),
            token);

        foreach (var (_, name, defaultCron) in handlers)
        {
            var job = JobBuilder.Create<CronJob>()
                .WithIdentity(name, "CronFunction")
                .Build();

            var cron = _configuration[name] ?? defaultCron;

            var trigger = TriggerBuilder.Create()
                .WithIdentity(name, "CronFunction")
                .WithCronSchedule(cron)
                .Build();

            await _scheduler.ScheduleJob(job, trigger, token);
            LogCronJobScheduled(_logger, name);
        }

        await _scheduler.Start(token);
    }
    public override Task StopAsync(CancellationToken token) => _scheduler?.Shutdown(token) ?? Task.CompletedTask;


    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Cron job {Name} scheduled")]
    private static partial void LogCronJobScheduled(ILogger logger, string name);

    #endregion
}