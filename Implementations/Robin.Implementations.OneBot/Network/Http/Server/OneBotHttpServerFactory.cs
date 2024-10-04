using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Communication;

namespace Robin.Implementations.OneBot.Network.Http.Server;

[Backend("OneBotHttpServer")]
// ReSharper disable once UnusedType.Global
public partial class OneBotHttpServerFactory(
    IServiceProvider provider,
    ILogger<OneBotHttpServerFactory> logger
) : IBackendFactory
{
    private static readonly Dictionary<int, OneBotHttpServerService> _services = [];

    private async Task<OneBotHttpServerService> GetServiceAsync(IConfiguration config, CancellationToken token)
    {
        var option = config.Get<OneBotHttpServerOption>()!;

        if (_services.TryGetValue(option.Port, out var service))
        {
            return service;
        }

        service = new OneBotHttpServerService(provider, option);
        await service.StartAsync(token);
        _services[option.Port] = service;
        return service;
    }

    public async Task<IBotEventInvoker> GetBotEventInvokerAsync(IConfiguration config, CancellationToken token)
    {
        LogGetBotEventInvoker(logger);
        return await GetServiceAsync(config, token);
    }

    public Task<IOperationProvider> GetOperationProviderAsync(IConfiguration config, CancellationToken token) =>
        throw new NotSupportedException("Http server does not support sending operations.");

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "GetBotEventInvoker")]
    private static partial void LogGetBotEventInvoker(ILogger logger);

    #endregion
}
