using System.Text.Json.Serialization;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Responses;
using Robin.Implementations.OneBot.Converter;
using Robin.Implementations.OneBot.Entity.Operations.Requests;

namespace Robin.Implementations.OneBot.Entity.Operations.Responses;

[Serializable]
[OneBotResponseData(typeof(OneBotSendPrivateMessageRequest))]
internal class OneBotSendPrivateMessageResponseData : IOneBotResponseData
{
    [JsonPropertyName("message_id")] public int MessageId { get; set; }

    public Response ToResponse(OneBotResponse response, OneBotMessageConverter converter) =>
        new SendPrivateMessageResponse(response.Status is not "failed", response.ReturnCode, null, MessageId);
}
