using System.Net;
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

namespace Robin.Implementations.OneBot.Network.WebSocket.Reverse;

internal partial class OneBotReverseWebSocketService(
    IServiceProvider service,
    OneBotReverseWebSocketOption options
) : BackgroundService, IBotEventInvoker
{
    private readonly ILogger<OneBotReverseWebSocketService> _logger =
        service.GetRequiredService<ILogger<OneBotReverseWebSocketService>>();

    private readonly OneBotMessageConverter _messageConverter =
        new(service.GetRequiredService<ILogger<OneBotMessageConverter>>());

    private readonly OneBotEventConverter _eventConverter =
        new(service.GetRequiredService<ILogger<OneBotEventConverter>>());

    private System.Net.WebSockets.WebSocket? _websocket;

    public event Func<BotEvent, CancellationToken, Task>? OnEventAsync;

    private async Task DispatchMessageAsync(string message, CancellationToken token)
    {
        var node = JsonNode.Parse(message);
        if (node is null) return;

        if (_eventConverter.ParseBotEvent(node, _messageConverter) is not { } @event)
            return;

        if (OnEventAsync is not null) await OnEventAsync.Invoke(@event, token);
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
                    _ = DispatchMessageAsync(message, token);
                    buffer = new byte[1024];
                    received = 0;
                }
                else if (received == buffer.Length)
                {
                    Array.Resize(ref buffer, received << 2);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://*:{options.Port}/");
        listener.Start();

        OnEventAsync += KeepAliveAsync;

        while (!token.IsCancellationRequested)
        {
            var context = await listener.GetContextAsync();

            if (!string.IsNullOrEmpty(options.AccessToken) &&
                context.Request.Headers["Authorization"] != $"Bearer {options.AccessToken}")
            {
                context.Response.StatusCode = 401;
                context.Response.Close();
            }
            else if (context.Request.IsWebSocketRequest)
            {
                var wsContext = await context.AcceptWebSocketAsync(null);
                _websocket = wsContext.WebSocket;
                LogConnected(_logger, context.Request.RemoteEndPoint);
                try
                {
                    await ReceiveLoop(token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (WebSocketException e) when (e.InnerException is HttpRequestException)
                {
                    LogWebSocketException(_logger, e);
                }
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }

        OnEventAsync -= KeepAliveAsync;
    }

    #region Log

    [LoggerMessage(Level = LogLevel.Debug, Message = "Receive message: {Message}")]
    private static partial void LogReceiveMessage(ILogger logger, string message);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Websocket throws an exception")]
    private static partial void LogWebSocketException(ILogger logger, Exception e);

    [LoggerMessage(Level = LogLevel.Information, Message = "Connection from {Src}")]
    private static partial void LogConnected(ILogger logger, IPEndPoint src);

    #endregion
}
