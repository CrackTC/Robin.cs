using Quartz;
using Quartz.Spi;

namespace Robin.Extensions.UserRank;

internal class UserRankJobFactory(IJob job) : IJobFactory
{
    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler) => job;

    public void ReturnJob(IJob _)
    {
    }
}