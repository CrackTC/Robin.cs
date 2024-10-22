using Quartz;

namespace Robin.Middlewares.Annotations.Cron;

public class CronJob(ICronHandler handler, CancellationToken token) : IJob
{
    public Task Execute(IJobExecutionContext context) => handler.OnCronEventAsync(token);
}
