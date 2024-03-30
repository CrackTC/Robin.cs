using Microsoft.Extensions.Configuration;

namespace Robin.Abstractions.Communication;

// keyed, scoped
public interface IBackendFactory
{
    Task<IBotEventInvoker> GetBotEventInvokerAsync(IConfiguration config, CancellationToken token);
    Task<IOperationProvider> GetOperationProviderAsync(IConfiguration config, CancellationToken token);
}