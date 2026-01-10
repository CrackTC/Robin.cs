using System;

namespace Robin.Implementations.Milky;

[Serializable]
internal class MilkyClientOption
{
    public required string Url { get; set; }
    public int ReconnectInterval { get; set; }
    public string? AccessToken { get; set; }
    public int SendMessageMaxRetry { get; set; } = 10;
}
