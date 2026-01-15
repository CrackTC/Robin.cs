using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Utility;
using Robin.Implementations.OneBot.Converter;
using Robin.Implementations.OneBot.Converter.Operation;
using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Network.Http.Client;

internal partial class OneBotHttpClientService(
    IServiceProvider service,
    OneBotHttpClientOption options
) : IOperationProvider
{
    private readonly ILogger<OneBotHttpClientService> _logger =
        service.GetRequiredService<ILogger<OneBotHttpClientService>>();

    private readonly OneBotMessageConverter _messageConverter =
        new(service.GetRequiredService<ILogger<OneBotMessageConverter>>());

    private readonly OneBotOperationConverterProvider _opConvProvider =
        new(options.OneBotVariant, service.GetRequiredService<ILogger<OneBotOperationConverterProvider>>());

    private readonly HttpClient _client = new();

    private readonly SemaphoreSlim _semaphore = new(options.RequestParallelism, options.RequestParallelism);

    public async Task<TResp> SendRequestAsync<TResp>(RequestFor<TResp> request, CancellationToken token) where TResp : Response
    {
        var reqConverter = _opConvProvider.GetRequestConverter(request);
        var respConverter = _opConvProvider.GetResponseConverter(request);
        var obReq = reqConverter.ConvertToOneBotRequest(request, _messageConverter);

        LogSendingData(_logger, obReq);

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{options.Url}/{obReq.Endpoint}")
        {
            Content = JsonContent.Create(obReq.Params)
        };

        if (!string.IsNullOrEmpty(options.AccessToken))
        {
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);
        }

        var response = await _semaphore.ConsumeAsync(() => _client.SendAsync(requestMessage, token), token);
        if (!response.IsSuccessStatusCode)
        {
            LogSendFailed(_logger);
            throw new();
        }

        return await respConverter.ConvertFromResponseStream(await response.Content.ReadAsStreamAsync(token), _messageConverter, token);
    }

    public void Dispose() => _client.Dispose();

    #region Log

    [LoggerMessage(Level = LogLevel.Trace, Message = "Send: {Data}")]
    private static partial void LogSendingData(ILogger logger, OneBotRequest data);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Send data failed")]
    private static partial void LogSendFailed(ILogger logger);

    #endregion
}
