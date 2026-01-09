using System.Text.Json.Serialization;
using Robin.Abstractions.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Responses;
using Robin.Implementations.OneBot.Converter;
using Robin.Implementations.OneBot.Entity.Operations.Requests;

namespace Robin.Implementations.OneBot.Entity.Operations.Responses;

using OneBotRequestType = OneBotGetGroupMemberInfoRequest;
using ResponseType = GetGroupMemberInfoResponse;

[Serializable]
[OneBotResponseData(typeof(OneBotRequestType))]
internal class OneBotGetGroupMemberInfoResponseData : IOneBotResponseData
{
    [JsonPropertyName("group_id")] public long GroupId { get; set; }
    [JsonPropertyName("user_id")] public long UserId { get; set; }
    [JsonPropertyName("nickname")] public required string Nickname { get; set; }
    [JsonPropertyName("card")] public required string Card { get; set; }
    [JsonPropertyName("sex")] public required string Sex { get; set; }
    [JsonPropertyName("age")] public int Age { get; set; }
    [JsonPropertyName("area")] public required string Area { get; set; }
    [JsonPropertyName("join_time")] public int JoinTime { get; set; }
    [JsonPropertyName("last_sent_time")] public int LastSentTime { get; set; }
    [JsonPropertyName("level")] public required string Level { get; set; }
    [JsonPropertyName("role")] public required string Role { get; set; }
    [JsonPropertyName("unfriendly")] public bool Unfriendly { get; set; }
    [JsonPropertyName("title")] public required string Title { get; set; }

    [JsonPropertyName("title_expire_time")]
    public int TitleExpireTime { get; set; }

    [JsonPropertyName("card_changeable")] public bool CardChangeable { get; set; }

    public Response ToResponse(OneBotResponse response, OneBotMessageConverter _) =>
        new ResponseType(
            response.Status is not "failed",
            response.ReturnCode,
            null,
            new GroupMemberInfo(
                GroupId,
                UserId,
                Nickname,
                Card,
                Sex switch
                {
                    "male" => UserSex.Male,
                    "female" => UserSex.Female,
                    _ => UserSex.Unknown
                },
                Age,
                Area,
                JoinTime,
                LastSentTime,
                string.IsNullOrEmpty(Level) ? null : int.Parse(Level),
                Role switch
                {
                    "owner" => GroupMemberRole.Owner,
                    "admin" => GroupMemberRole.Admin,
                    _ => GroupMemberRole.Member
                },
                Unfriendly,
                Title,
                TitleExpireTime,
                CardChangeable
            )
        );
}
