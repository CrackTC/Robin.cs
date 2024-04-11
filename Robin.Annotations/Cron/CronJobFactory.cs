using Quartz;
using Quartz.Spi;

namespace Robin.Annotations.Cron;

public class CronJobFactory(IReadOnlyDictionary<string, ICronHandler> handlers, CancellationToken token = default) : IJobFactory
{
    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler) =>
        new CronJob(handlers[bundle.JobDetail.Key.Name], token);

    public void ReturnJob(IJob job)
    {
    }
}