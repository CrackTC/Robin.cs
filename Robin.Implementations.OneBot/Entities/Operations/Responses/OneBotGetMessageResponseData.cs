using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Robin.Abstractions.Entities;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Responses;
using Robin.Implementations.OneBot.Converters;
using Robin.Implementations.OneBot.Entities.Common;
using Robin.Implementations.OneBot.Entities.Operations.Requests;

namespace Robin.Implementations.OneBot.Entities.Operations.Responses;

[Serializable]
[OneBotResponseData(typeof(OneBotGetMessageRequest))]
internal class OneBotGetMessageResponseData : IOneBotResponseData
{
    [JsonPropertyName("time")] public int Time { get; set; }
    [JsonPropertyName("message_type")] public required string MessageType { get; set; }
    [JsonPropertyName("message_id")] public int MessageId { get; set; }
    [JsonPropertyName("real_id")] public int RealId { get; set; }
    [JsonPropertyName("sender")] public required OneBotGroupMessageSender Sender { get; set; }
    [JsonPropertyName("message")] public required JsonNode Message { get; set; }

    public Response ToResponse(OneBotResponse response, OneBotMessageConverter converter) =>
        new GetMessageResponse(
            response.Status is not "failed",
            response.ReturnCode,
            null,
            new MessageInfo(
                Time,
                MessageType,
                MessageId,
                RealId,
                new GroupMessageSender(
                    Sender.UserId,
                    Sender.Nickname,
                    Sender.Card,
                    Sender.Sex switch
                    {
                        "male" => UserSex.Male,
                        "female" => UserSex.Female,
                        _ => UserSex.Unknown
                    },
                    Sender.Age,
                    Sender.Area,
                    Sender.Level,
                    Sender.Role switch
                    {
                        "owner" => GroupMemberRole.Owner,
                        "admin" => GroupMemberRole.Admin,
                        _ => GroupMemberRole.Member
                    },
                    Sender.Title
                ),
                converter.ParseMessageChain(Message) ?? []
            )
        );
}