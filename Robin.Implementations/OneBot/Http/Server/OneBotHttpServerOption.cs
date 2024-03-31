namespace Robin.Implementations.OneBot.Http.Server;

[Serializable]
internal class OneBotHttpServerOption
{
    public int Port { get; set; }
    public string? Secret { get; set; }
}