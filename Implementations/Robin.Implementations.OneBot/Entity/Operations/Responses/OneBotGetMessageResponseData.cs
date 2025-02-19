using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Robin.Abstractions.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Responses;
using Robin.Implementations.OneBot.Converter;
using Robin.Implementations.OneBot.Entity.Common;
using Robin.Implementations.OneBot.Entity.Operations.Requests;

namespace Robin.Implementations.OneBot.Entity.Operations.Responses;

using OneBotRequestType = OneBotGetMessageRequest;
using ResponseType = GetMessageResponse;

[Serializable]
[OneBotResponseData(typeof(OneBotRequestType))]
internal class OneBotGetMessageResponseData : IOneBotResponseData
{
    [JsonPropertyName("time")] public int Time { get; set; }
    [JsonPropertyName("message_type")] public required string MessageType { get; set; }
    [JsonPropertyName("message_id")] public int MessageId { get; set; }
    [JsonPropertyName("real_id")] public int RealId { get; set; }
    [JsonPropertyName("sender")] public required OneBotGroupMessageSender Sender { get; set; }
    [JsonPropertyName("message")] public required JsonNode Message { get; set; }

    public Response ToResponse(OneBotResponse response, OneBotMessageConverter converter) =>
        new ResponseType(
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
