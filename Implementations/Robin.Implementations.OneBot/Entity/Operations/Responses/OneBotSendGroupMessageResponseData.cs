using System.Text.Json.Serialization;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Responses;
using Robin.Implementations.OneBot.Converter;
using Robin.Implementations.OneBot.Entity.Operations.Requests;

namespace Robin.Implementations.OneBot.Entity.Operations.Responses;

[Serializable]
[OneBotResponseData(typeof(OneBotSendGroupMessageRequest))]
internal class OneBotSendGroupMessageResponseData : IOneBotResponseData
{
    [JsonPropertyName("message_id")] public int MessageId { get; set; }

    public Response ToResponse(OneBotResponse response, OneBotMessageConverter _) =>
        new SendGroupMessageResponse(response.Status is not "failed", response.ReturnCode, null, MessageId);
}
