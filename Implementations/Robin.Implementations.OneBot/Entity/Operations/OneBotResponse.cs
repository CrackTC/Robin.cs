using System.Text.Json.Serialization;

namespace Robin.Implementations.OneBot.Entity.Operations;

internal class OneBotResponse<TData>
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("retcode")]
    public int ReturnCode { get; set; }

    [JsonPropertyName("data")]
    public TData? Data { get; set; }

    [JsonPropertyName("echo")]
    public string? Echo { get; set; }
}
