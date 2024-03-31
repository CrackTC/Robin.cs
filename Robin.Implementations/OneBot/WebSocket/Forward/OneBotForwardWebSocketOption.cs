namespace Robin.Implementations.OneBot.WebSocket.Forward;

[Serializable]
internal class OneBotWebSocketOption
{
    public string Url { get; set; } = string.Empty;
    public int ReconnectInterval { get; set; }
    public string? AccessToken { get; set; }
}