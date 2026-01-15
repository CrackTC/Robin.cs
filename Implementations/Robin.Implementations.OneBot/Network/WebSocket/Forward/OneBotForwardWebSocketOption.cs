namespace Robin.Implementations.OneBot.Network.WebSocket.Forward;

internal class OneBotForwardWebSocketOption
{
    public required string Url { get; set; }
    public int ReconnectInterval { get; set; }
    public string? AccessToken { get; set; }
}
