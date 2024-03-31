using Microsoft.Data.Sqlite;
using Quartz;
using Quartz.Spi;
using Robin.Abstractions.Communication;

namespace Robin.Extensions.UserRank;

internal class UserRankJobFactory(
    IServiceProvider service,
    IOperationProvider operation,
    SqliteConnection connection,
    UserRankOption option
) : IJobFactory
{
    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        => new UserRankJob(service, operation, connection, option);

    public void ReturnJob(IJob job)
    {
    }
}