using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Communication;

namespace Robin.Implementations.OneBot.Network.WebSocket.Reverse;

[Backend("OneBotReverseWebSocket")]
public partial class OneBotReverseWebSocketFactory(
    IServiceProvider provider,
    ILogger<OneBotReverseWebSocketFactory> logger
) : IBackendFactory
{
    public async Task<IBotEventInvoker> GetBotEventInvokerAsync(
        IConfiguration config,
        CancellationToken token
    )
    {
        LogGetBotEventInvoker(logger);
        var option = config.Get<OneBotReverseWebSocketOption>()!;
        var service = new OneBotReverseWebSocketService(provider, option);
        await service.StartAsync(token);
        return service;
    }

    public async Task<IOperationProvider> GetOperationProviderAsync(
        IConfiguration config,
        CancellationToken token
    )
    {
        throw new NotSupportedException();
    }

    #region Log

    [LoggerMessage(Level = LogLevel.Debug, Message = "GetBotEventInvoker")]
    private static partial void LogGetBotEventInvoker(ILogger logger);

    #endregion
}
