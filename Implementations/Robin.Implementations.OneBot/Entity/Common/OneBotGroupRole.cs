using System.Text.Json.Serialization;
using Robin.Abstractions.Entity;

namespace Robin.Implementations.OneBot.Entity.Common;

internal enum OneBotGroupRole
{
    [JsonPropertyName("member")]
    Member,

    [JsonPropertyName("admin")]
    Admin,

    [JsonPropertyName("owner")]
    Owner,
}

internal static class OneBotGroupRoleExtensions
{
    public static GroupMemberRole ToGroupMemberRole(this OneBotGroupRole role) =>
        role switch
        {
            OneBotGroupRole.Owner => GroupMemberRole.Owner,
            OneBotGroupRole.Admin => GroupMemberRole.Admin,
            OneBotGroupRole.Member => GroupMemberRole.Member,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
        };
}
