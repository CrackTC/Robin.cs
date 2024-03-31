namespace Robin.Implementations.OneBot.WebSocket.Reverse;

[Serializable]
internal class OneBotReverseWebSocketOption
{
    public int Port { get; set; }
    public string? AccessToken { get; set; }
}