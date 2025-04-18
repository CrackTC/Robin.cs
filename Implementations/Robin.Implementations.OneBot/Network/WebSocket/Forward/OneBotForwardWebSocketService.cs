using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Meta;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Utility;
using Robin.Implementations.OneBot.Converter;
using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Network.WebSocket.Forward;

internal partial class OneBotForwardWebSocketService(
    IServiceProvider service,
    OneBotForwardWebSocketOption options
) : BackgroundService, IBotEventInvoker, IOperationProvider
{
    private readonly ILogger<OneBotForwardWebSocketService> _logger =
        service.GetRequiredService<ILogger<OneBotForwardWebSocketService>>();

    private readonly OneBotMessageConverter _messageConverter =
        new(service.GetRequiredService<ILogger<OneBotMessageConverter>>());

    private readonly OneBotEventConverter _eventConverter =
        new(service.GetRequiredService<ILogger<OneBotEventConverter>>());

    private readonly OneBotOperationConverter _operationConverter =
        new(service.GetRequiredService<ILogger<OneBotOperationConverter>>());

    private ClientWebSocket? _websocket;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public event Func<BotEvent, CancellationToken, Task>? OnEventAsync;

    public async Task<TResp?> SendRequestAsync<TResp>(RequestFor<TResp> request, CancellationToken token) where TResp : Response
    {
        if (_operationConverter.SerializeToJson(request, _messageConverter) is not { } pair)
        {
            LogInvalidRequest(_logger, request.ToString());
            return null;
        }

        var (endpoint, r, type) = pair;

        var echo = Guid.NewGuid().ToString();
        var json = JsonSerializer.Serialize(new OneBotWebSocketRequest
        {
            Action = endpoint,
            Params = r!,
            Echo = echo
        });
        LogSendingData(_logger, json);

        var buffer = Encoding.UTF8.GetBytes(json);

        try
        {
            await _semaphore.ConsumeAsync(() => _websocket!.SendAsync(buffer.AsMemory(), WebSocketMessageType.Text, true, token), token);

            var completionSource = new TaskCompletionSource<TResp?>();

            Action<OneBotResponse> onResponse = null!;
            onResponse = oneBotResponse =>
            {
                if (oneBotResponse.Echo != echo) return;
                OnResponse -= onResponse;
                var response = _operationConverter.ParseResponse(type, oneBotResponse, _messageConverter);
                completionSource.SetResult(response as TResp);
            };

            OnResponse += onResponse;
            return await completionSource.Task;
        }
        catch (Exception e)
        {
            LogSendFailed(_logger, e);
            return null;
        }
    }

    private event Action<OneBotResponse>? OnResponse;

    private async void DispatchMessageAsync(string message, CancellationToken token)
    {
        try
        {
            var node = JsonNode.Parse(message);
            if (node is null) return;

            if (node["post_type"] is null)
            {
                if (node.Deserialize<OneBotResponse>() is not { } response)
                {
                    LogInvalidResponse(_logger, message);
                    return;
                }

                OnResponse?.Invoke(response);
                return;
            }

            if (_eventConverter.ParseBotEvent(node, _messageConverter) is not { } @event)
                return;

            if (OnEventAsync is not null) await OnEventAsync.Invoke(@event, token);
        }
        catch (Exception e)
        {
            LogDispatchException(_logger, message, e);
        }
    }

    private async Task KeepAliveAsync(BotEvent @event, CancellationToken token)
    {
        if (@event is not HeartbeatEvent e) return;

        var alive = false;
        Func<BotEvent, CancellationToken, Task> setAlive = null!;

        setAlive = (_, _) =>
        {
            alive = true;
            OnEventAsync -= setAlive;
            return Task.CompletedTask;
        };

        OnEventAsync += setAlive;

        await Task.Delay(TimeSpan.FromMilliseconds(e.Interval * 3), token);

        if (!alive) _websocket!.Abort();
    }

    private async Task ReceiveLoop(CancellationToken token)
    {
        var buffer = new byte[1024];
        var received = 0;

        try
        {
            while (true)
            {
                var result = await _websocket!.ReceiveAsync(buffer.AsMemory(received), token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close", token);
                    break;
                }

                received += result.Count;
                if (result.EndOfMessage)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, received);
                    LogReceiveMessage(_logger, message);
                    DispatchMessageAsync(message, token);
                    buffer = new byte[1024];
                    received = 0;
                }
                else if (received == buffer.Length)
                {
                    Array.Resize(ref buffer, received << 2);
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // ignore
        }
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        if (!Uri.TryCreate(options.Url, UriKind.Absolute, out var uri))
        {
            LogInvalidUri(_logger, options.Url);
            return;
        }

        OnEventAsync += KeepAliveAsync;

        while (true)
        {
            _websocket = new ClientWebSocket();
            try
            {
                if (!string.IsNullOrEmpty(options.AccessToken))
                    _websocket.Options.SetRequestHeader("Authorization", $"Bearer {options.AccessToken}");

                LogConnecting(_logger, options.Url);
                await _websocket.ConnectAsync(uri, token);
                LogConnected(_logger, options.Url);

                await ReceiveLoop(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (WebSocketException e)
            {
                LogWebSocketException(_logger, e);
                LogReconnect(_logger, options.ReconnectInterval);
                var interval = TimeSpan.FromSeconds(options.ReconnectInterval);
                await Task.Delay(interval, token);
            }
            catch (ObjectDisposedException)
            {
                LogReconnect(_logger, options.ReconnectInterval);
                var interval = TimeSpan.FromSeconds(options.ReconnectInterval);
                await Task.Delay(interval, token);
            }
        }

        OnEventAsync -= KeepAliveAsync;
    }

    #region Log

    [LoggerMessage(Level = LogLevel.Error, Message = "Invalid URI: {Uri}")]
    private static partial void LogInvalidUri(ILogger logger, string uri);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Receive message: {Message}")]
    private static partial void LogReceiveMessage(ILogger logger, string message);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid response: {Response}")]
    private static partial void LogInvalidResponse(ILogger logger, string response);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid request: {Request}")]
    private static partial void LogInvalidRequest(ILogger logger, string request);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Connection closed, reconnect after {Interval} seconds")]
    private static partial void LogReconnect(ILogger logger, int interval);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Send: {Data}")]
    private static partial void LogSendingData(ILogger logger, string data);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Send data failed")]
    private static partial void LogSendFailed(ILogger logger, Exception e);

    [LoggerMessage(Level = LogLevel.Information, Message = "Connected to {Uri}")]
    private static partial void LogConnected(ILogger logger, string uri);

    [LoggerMessage(Level = LogLevel.Information, Message = "Connecting to {Uri}")]
    private static partial void LogConnecting(ILogger logger, string uri);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Websocket throws an exception")]
    private static partial void LogWebSocketException(ILogger logger, Exception e);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Exception occured while dispatching message: {Message}")]
    private static partial void LogDispatchException(ILogger logger, string message, Exception e);

    #endregion
}
