using System.Text.Json.Serialization;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Responses;
using Robin.Implementations.OneBot.Converter;
using Robin.Implementations.OneBot.Entity.Operations.Requests;

namespace Robin.Implementations.OneBot.Entity.Operations.Responses;

using OneBotRequestType = OneBotSendGroupForwardMessageRequest;
using ResponseType = SendGroupForwardMessageResponse;

[Serializable]
[OneBotResponseData(typeof(OneBotRequestType))]
internal class OneBotSendGroupForwardMessageResponseData : IOneBotResponseData
{
    [JsonPropertyName("message_id")] public required int MessageId { get; set; }
    [JsonPropertyName("forward_id")] public required string ForwardId { get; set; }

    public Response ToResponse(OneBotResponse response, OneBotMessageConverter _)
        => new ResponseType(response.Status is not "failed", response.ReturnCode, null, new(MessageId.ToString(), ForwardId));
}
