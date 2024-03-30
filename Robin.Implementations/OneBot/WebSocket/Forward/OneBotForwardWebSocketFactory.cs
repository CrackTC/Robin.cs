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

    private OneBotForwardWebSocketService GetService(IConfiguration config)
    {
        var option = new OneBotWebSocketOption();
        config.Bind(option);

        if (_services.TryGetValue(option.Url, out var service))
        {
            return service;
        }

        service = new OneBotForwardWebSocketService(provider, option);
        _services[option.Url] = service;
        return service;
    }

    public IBotEventInvoker GetBotEventInvoker(IConfiguration config)
    {
        LogGetBotEventInvoker(logger);
        return GetService(config);
    }

    public IOperationProvider GetOperationProvider(IConfiguration config)
    {
        LogGetOperationProvider(logger);
        return GetService(config);
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "GetBotEventInvoker")]
    private static partial void LogGetBotEventInvoker(ILogger logger);

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "GetOperationProvider")]
    private static partial void LogGetOperationProvider(ILogger logger);

    #endregion
}