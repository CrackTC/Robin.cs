namespace Robin.Implementations.OneBot.Network.Http.Server;

[Serializable]
internal class OneBotHttpServerOption
{
    public int Port { get; set; }
    public string? Secret { get; set; }
}