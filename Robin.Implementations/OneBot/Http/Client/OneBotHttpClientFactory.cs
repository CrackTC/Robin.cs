using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Communication;

namespace Robin.Implementations.OneBot.Http.Client;

[Backend("OneBotHttpClient")]
// ReSharper disable once UnusedType.Global
public partial class OneBotHttpClientFactory(
    IServiceProvider provider,
    ILogger<OneBotHttpClientFactory> logger
) : IBackendFactory
{
    private static readonly Dictionary<string, OneBotHttpClientService> _services = [];

    private OneBotHttpClientService GetService(IConfiguration config)
    {
        var option = config.Get<OneBotHttpClientOption>()!;

        if (_services.TryGetValue(option.Url, out var service))
        {
            return service;
        }

        service = new OneBotHttpClientService(provider, option);
        _services[option.Url] = service;
        return service;
    }

    public Task<IBotEventInvoker> GetBotEventInvokerAsync(IConfiguration config, CancellationToken token) =>
        throw new NotSupportedException("Http client does not support receiving events.");

    public Task<IOperationProvider> GetOperationProviderAsync(IConfiguration config, CancellationToken token)
    {
        LogGetOperationProvider(logger);
        return Task.FromResult<IOperationProvider>(GetService(config));
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "GetOperationProvider")]
    private static partial void LogGetOperationProvider(ILogger logger);

    #endregion
}