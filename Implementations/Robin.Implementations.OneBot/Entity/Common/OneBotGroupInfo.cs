using System.Text.Json.Serialization;
using Robin.Abstractions.Entity;

namespace Robin.Implementations.OneBot.Entity.Common;

internal class OneBotGroupInfo
{
    [JsonPropertyName("group_id")] public long GroupId { get; set; }
    [JsonPropertyName("group_name")] public string GroupName { get; set; } = string.Empty;
    [JsonPropertyName("member_count")] public int MemberCount { get; set; }
    [JsonPropertyName("max_member_count")] public int MaxMemberCount { get; set; }

    public GroupInfo ToGroupInfo() => new(GroupId, GroupName, MemberCount, MaxMemberCount);
}
