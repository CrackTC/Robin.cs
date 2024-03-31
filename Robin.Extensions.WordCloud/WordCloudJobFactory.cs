using Microsoft.Data.Sqlite;
using Quartz;
using Quartz.Spi;
using Robin.Abstractions.Communication;

namespace Robin.Extensions.WordCloud;

internal class WordCloudJobFactory(
    IServiceProvider service,
    IOperationProvider operation,
    SqliteConnection connection,
    WordCloudOption option
) : IJobFactory
{
    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        => new WordCloudJob(service, operation, connection, option);

    public void ReturnJob(IJob job)
    {
    }
}