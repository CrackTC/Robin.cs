namespace Robin.Implementations.OneBot.Http.Client;

[Serializable]
internal class OneBotHttpClientOption
{
    public required string Url { get; set; }
    public string? AccessToken { get; set; }
}