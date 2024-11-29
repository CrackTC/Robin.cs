using Quartz;
using Robin.Middlewares.Fluent.Tunnel;

namespace Robin.Middlewares.Fluent.Cron;

internal class CronJob(Tunnel<CancellationToken, Task> tunnel, CancellationToken token) : IJob
{
    public Task Execute(IJobExecutionContext context) =>
        tunnel(token) is { Accept: true, Data: { } data } ? data : Task.CompletedTask;
}
