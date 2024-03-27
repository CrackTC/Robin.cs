using Microsoft.Extensions.Configuration;

namespace Robin.Abstractions.Communication;

// keyed, scoped
public interface IBackendFactory
{
    IBotEventInvoker GetBotEventInvoker(IConfiguration config);
    IOperationProvider GetOperationProvider(IConfiguration config);
}