using Microsoft.Extensions.Hosting;

namespace Robin.Services;

public class BotLifetimeService : IHostedService
{
    public long Uin { get; }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}