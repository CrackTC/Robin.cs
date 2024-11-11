using System.Text.Json.Serialization;
using Robin.Abstractions.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Responses;
using Robin.Implementations.OneBot.Converter;
using Robin.Implementations.OneBot.Entity.Operations.Requests;

namespace Robin.Implementations.OneBot.Entity.Operations.Responses;

[Serializable]
[OneBotResponseData(typeof(OneBotGetGroupInfoRequest))]
internal class OneBotGetGroupInfoResponseData : IOneBotResponseData
{
    [JsonPropertyName("group_id")] public long GroupId { get; set; }
    [JsonPropertyName("group_name")] public required string GroupName { get; set; }
    [JsonPropertyName("member_count")] public int MemberCount { get; set; }
    [JsonPropertyName("max_member_count")] public int MaxMemberCount { get; set; }

    public Response ToResponse(OneBotResponse response, OneBotMessageConverter _) =>
        new GetGroupInfoResponse(
            response.Status is not "failed",
            response.ReturnCode,
            null,
            new GroupInfo(
                GroupId,
                GroupName,
                MemberCount,
                MaxMemberCount
            )
        );
}
