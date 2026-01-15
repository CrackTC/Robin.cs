using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Meta;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Network.WebSocket.Forward;

internal partial class OneBotForwardWebSocketService(
    IServiceProvider service,
    OneBotForwardWebSocketOption options
) : BackgroundService, IBotEventInvoker
{
    private readonly ILogger<OneBotForwardWebSocketService> _logger =
        service.GetRequiredService<ILogger<OneBotForwardWebSocketService>>();

    private readonly OneBotMessageConverter _messageConverter =
        new(service.GetRequiredService<ILogger<OneBotMessageConverter>>());

    private readonly OneBotEventConverter _eventConverter =
        new(service.GetRequiredService<ILogger<OneBotEventConverter>>());

    private ClientWebSocket? _websocket;

    public event Func<BotEvent, CancellationToken, Task>? OnEventAsync;

    private async void DispatchMessageAsync(string message, CancellationToken token)
    {
        try
        {
            var node = JsonNode.Parse(message);
            if (node is null) return;

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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Connection closed, reconnect after {Interval} seconds")]
    private static partial void LogReconnect(ILogger logger, int interval);

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
