using Quartz;
using Quartz.Spi;
using Robin.Middlewares.Fluent.Tunnel;

namespace Robin.Middlewares.Fluent.Cron;

internal class KeyedCronJobFactory(
    IReadOnlyDictionary<(string Group, string Name), Tunnel<CancellationToken, Task>> tunnels,
    CancellationToken token
) : IJobFactory
{
    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler) =>
        new CronJob(tunnels[(bundle.JobDetail.Key.Group, bundle.JobDetail.Key.Name)], token);

    public void ReturnJob(IJob job) { }
}
