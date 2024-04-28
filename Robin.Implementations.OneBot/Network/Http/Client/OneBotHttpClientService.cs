using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Operation;
using Robin.Implementations.OneBot.Converters;
using Robin.Implementations.OneBot.Entities.Operations;

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

    private readonly OneBotOperationConverter _operationConverter =
        new(service.GetRequiredService<ILogger<OneBotOperationConverter>>());

    private readonly HttpClient _client = new();

    private readonly SemaphoreSlim _semaphore = new(options.RequestParallelism, options.RequestParallelism);

    public async Task<Response?> SendRequestAsync(Request request, CancellationToken token = default)
    {
        if (_operationConverter.SerializeToJson(request, _messageConverter) is not { } pair)
        {
            LogInvalidRequest(_logger, request.ToString());
            return null;
        }

        var (endpoint, r, type) = pair;

        var json = r!.ToJsonString();
        LogSendingData(_logger, json);

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{options.Url}/{endpoint}")
        {
            Content = new StringContent(json, new MediaTypeHeaderValue("application/json"))
        };

        if (!string.IsNullOrEmpty(options.AccessToken))
        {
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);
        }

        HttpResponseMessage response;
        await _semaphore.WaitAsync(token);
        try
        {
            response = await _client.SendAsync(requestMessage, token);
        }
        finally
        {
            _semaphore.Release();
        }

        if (!response.IsSuccessStatusCode)
        {
            LogSendFailed(_logger);
            return null;
        }

        var oneBotResponse =
            await JsonSerializer.DeserializeAsync<OneBotResponse>(await response.Content.ReadAsStreamAsync(token),
                cancellationToken: token);

        if (oneBotResponse is not null)
            return _operationConverter.ParseResponse(type, oneBotResponse, _messageConverter);

        LogInvalidResponse(_logger, await response.Content.ReadAsStringAsync(token));
        return null;
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "Receive message: {Message}")]
    private static partial void LogReceiveMessage(ILogger logger, string message);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Invalid request: {Request}")]
    private static partial void LogInvalidRequest(ILogger logger, string request);

    [LoggerMessage(EventId = 2, Level = LogLevel.Trace, Message = "Send: {Data}")]
    private static partial void LogSendingData(ILogger logger, string data);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Send data failed")]
    private static partial void LogSendFailed(ILogger logger);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Invalid response: {Response}")]
    private static partial void LogInvalidResponse(ILogger logger, string response);

    #endregion
}