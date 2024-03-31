using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Communication;

namespace Robin.Implementations.OneBot.WebSocket.Reverse;

[Backend("OneBotReverseWebSocket")]
// ReSharper disable once UnusedType.Global
public partial class OneBotReverseWebSocketFactory(
    IServiceProvider provider,
    ILogger<OneBotReverseWebSocketFactory> logger
) : IBackendFactory
{
    private static readonly Dictionary<int, OneBotReverseWebSocketService> _services = [];

    private async Task<OneBotReverseWebSocketService> GetServiceAsync(IConfiguration config, CancellationToken token)
    {
        var option = config.Get<OneBotReverseWebSocketOption>()!;

        if (_services.TryGetValue(option.Port, out var service))
        {
            return service;
        }

        service = new OneBotReverseWebSocketService(provider, option);
        await service.StartAsync(token);
        _services[option.Port] = service;
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