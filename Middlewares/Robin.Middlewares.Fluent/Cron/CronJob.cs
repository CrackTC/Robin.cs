using Quartz;
using Robin.Middlewares.Fluent.Tunnel;

namespace Robin.Middlewares.Fluent.Cron;

internal class CronJob(Tunnel<CancellationToken, Task> tunnel, CancellationToken token) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        if (await tunnel(token) is { Accept: true, Data: { } data })
            await data;
    }
}
