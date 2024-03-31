using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Event;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Http.Server;

internal partial class OneBotHttpServerService(
    IServiceProvider service,
    OneBotHttpServerOption options
) : BackgroundService, IBotEventInvoker
{
    private readonly ILogger<OneBotHttpServerService> _logger =
        service.GetRequiredService<ILogger<OneBotHttpServerService>>();

    private readonly OneBotMessageConverter _messageConverter =
        new(service.GetRequiredService<ILogger<OneBotMessageConverter>>());

    private readonly OneBotEventConverter _eventConverter =
        new(service.GetRequiredService<ILogger<OneBotEventConverter>>());

    public event Action<BotEvent>? OnEvent;
    
    private HMACSHA1? _sha1;

    private void DispatchMessage(string message)
    {
        var node = JsonNode.Parse(message);
        if (node is null) return;

        if (_eventConverter.ParseBotEvent(node, _messageConverter) is not { } @event)
            return;
        OnEvent?.Invoke(@event);
    }
    
    private string ComputeSha1(string message)
    {
        var hash = _sha1!.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToHexString(hash).ToLower();
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://*:{options.Port}/");
        listener.Start();
        
        if (!string.IsNullOrEmpty(options.Secret))
        {
            _sha1 = new HMACSHA1(Encoding.UTF8.GetBytes(options.Secret));
        }

        while (!token.IsCancellationRequested)
        {
            var context = await listener.GetContextAsync();
            if (context.Request.HttpMethod != "POST")
            {
                context.Response.StatusCode = 405;
                context.Response.Close();
                continue;
            }
            
            var reader = new StreamReader(context.Request.InputStream);
            var message = await reader.ReadToEndAsync(token);
            LogReceiveMessage(_logger, message);
            
            if (!string.IsNullOrEmpty(options.Secret))
            {
                var signature = context.Request.Headers["X-Signature"];
                if (signature != $"sha1={ComputeSha1(message)}")
                {
                    context.Response.StatusCode = 401;
                    context.Response.Close();
                    continue;
                }
            }
            
            _ = Task.Run(() => DispatchMessage(message), token);
        }
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Receive message: {Message}")]
    private static partial void LogReceiveMessage(ILogger logger, string message);

    #endregion
}