namespace Robin.Implementations.OneBot.Network.Http.Client;

internal class OneBotHttpClientOption
{
    public required string Url { get; set; }
    public string? AccessToken { get; set; }
    public int RequestParallelism { get; set; } = 1;
    public string? OneBotVariant { get; set; }
}
