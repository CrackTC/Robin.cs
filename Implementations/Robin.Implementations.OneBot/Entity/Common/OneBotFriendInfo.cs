using System.Text.Json.Serialization;

namespace Robin.Implementations.OneBot.Entity.Common;

[Serializable]
internal class OneBotFriendInfo
{
    [JsonPropertyName("user_id")] public long UserId { get; set; }
    [JsonPropertyName("nickname")] public string Nickname { get; set; } = string.Empty;
    [JsonPropertyName("remark")] public string Remark { get; set; } = string.Empty;
}
