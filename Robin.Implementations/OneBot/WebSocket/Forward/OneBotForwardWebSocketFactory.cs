using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Communication;

namespace Robin.Implementations.OneBot.WebSocket.Forward;

[Backend("OneBotForwardWebSocket")]
// ReSharper disable once UnusedType.Global
public partial class OneBotForwardWebSocketFactory(
    IServiceProvider provider,
    ILogger<OneBotForwardWebSocketFactory> logger
) : IBackendFactory
{
    private static readonly Dictionary<string, OneBotForwardWebSocketService> _services = [];

    private async Task<OneBotForwardWebSocketService> GetServiceAsync(IConfiguration config, CancellationToken token)
    {
        var option = config.Get<OneBotWebSocketOption>()!;

        if (_services.TryGetValue(option.Url, out var service))
        {
            return service;
        }

        service = new OneBotForwardWebSocketService(provider, option);
        await service.StartAsync(token);
        _services[option.Url] = service;
        return service;
    }

    public async Task<IBotEventInvoker> GetBotEventInvokerAsync(IConfiguration config, CancellationToken token)
    {
        LogGetBotEventInvoker(logger);
        return await GetServiceAsync(config, token);
    }

    public async Task<IOperationProvider> GetOperationProviderAsync(IConfiguration config, CancellationToken token)
    {
        LogGetOperationProvider(logger);
        return await GetServiceAsync(config, token);
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "GetBotEventInvoker")]
    private static partial void LogGetBotEventInvoker(ILogger logger);

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "GetOperationProvider")]
    private static partial void LogGetOperationProvider(ILogger logger);

    #endregion
}