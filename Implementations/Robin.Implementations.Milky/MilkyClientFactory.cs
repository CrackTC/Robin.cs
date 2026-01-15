using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Communication;

namespace Robin.Implementations.Milky;

[Backend("MilkyClient")]
public class MilkyClientFactory(IServiceProvider provider, ILogger<MilkyClientFactory> logger)
    : IBackendFactory
{
    private static readonly Dictionary<string, MilkyClientService> _services = [];

    private async Task<MilkyClientService> GetServiceAsync(
        IConfiguration config,
        CancellationToken token
    )
    {
        var option = config.Get<MilkyClientOption>()!;

        if (_services.GetValueOrDefault(option.Url) is { } service)
            return service;

        service = new MilkyClientService(provider, option);
        await service.StartAsync(token);
        _services[option.Url] = service;
        return service;
    }

    public async Task<IBotEventInvoker> GetBotEventInvokerAsync(
        IConfiguration config,
        CancellationToken token
    )
    {
        logger.LogGetBotEventInvoker();
        return await GetServiceAsync(config, token);
    }

    public async Task<IOperationProvider> GetOperationProviderAsync(
        IConfiguration config,
        CancellationToken token
    )
    {
        logger.LogGetOperationProvider();
        return await GetServiceAsync(config, token);
    }
}

internal static partial class MilkyClientFactoryLoggerExtension
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "GetBotEventInvoker")]
    public static partial void LogGetBotEventInvoker(this ILogger<MilkyClientFactory> logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "GetOperationProvider")]
    public static partial void LogGetOperationProvider(this ILogger<MilkyClientFactory> logger);
}
