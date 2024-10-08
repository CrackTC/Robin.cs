using System.Collections.Frozen;
using System.Collections.Specialized;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using Robin.Abstractions;
using Robin.Abstractions.Context;

namespace Robin.Annotations.Cron;

[BotFunctionInfo("cron", "元功能，在指定的时间执行任务")]
// ReSharper disable UnusedType.Global
public partial class CronFunction(FunctionContext context) : BotFunction(context)
{
    private IScheduler? _scheduler;
    public override async Task StartAsync(CancellationToken token)
    {
        var handlers = _context.Functions
            .OfType<ICronHandler>()
            .Select(handler => (
                Handler: handler,
                InfoAttribute: handler.GetType().GetCustomAttribute<BotFunctionInfoAttribute>(),
                CronAttribute: handler.GetType().GetCustomAttribute<OnCronAttribute>()))
            .Where(tuple => tuple.InfoAttribute is not null && tuple.CronAttribute is not null)
            .Select(tuple => (tuple.Handler, tuple.InfoAttribute!.Name, tuple.CronAttribute!.Cron))
            .ToList();

        _scheduler = await new StdSchedulerFactory(new NameValueCollection
        {
            [StdSchedulerFactory.PropertySchedulerInstanceName] = $"Scheduler-{_context.Uin}"
        }).GetScheduler(token);

        _scheduler.JobFactory = new CronJobFactory(
            handlers.ToFrozenDictionary(tuple => $"{tuple.Name}-{_context.Uin}", tuple => tuple.Handler),
            token
        );

        foreach (var (_, name, defaultCron) in handlers)
        {
            var job = JobBuilder.Create<CronJob>()
                .WithIdentity($"{name}-{_context.Uin}", "CronFunction")
                .Build();

            var cron = _context.Configuration[name] ?? defaultCron;

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"{name}-{_context.Uin}", "CronFunction")
                .WithCronSchedule(cron)
                .Build();

            await _scheduler.ScheduleJob(job, trigger, token);
            LogCronJobScheduled(_context.Logger, name);
        }

        await _scheduler.Start(token);
    }
    public override Task StopAsync(CancellationToken token) => _scheduler?.Shutdown(token) ?? Task.CompletedTask;


    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Cron job {Name} scheduled")]
    private static partial void LogCronJobScheduled(ILogger logger, string name);

    #endregion
}
