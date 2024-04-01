using Quartz;
using Quartz.Spi;

namespace Robin.Extensions.WordCloud;

internal class WordCloudJobFactory(IJob job) : IJobFactory
{
    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler) => job;

    public void ReturnJob(IJob _)
    {
    }
}