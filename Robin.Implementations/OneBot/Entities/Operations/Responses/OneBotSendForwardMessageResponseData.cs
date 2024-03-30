using System.Text.Json.Serialization;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Responses;
using Robin.Implementations.OneBot.Entities.Operations.Requests;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Entities.Operations.Responses;

[Serializable]
[OneBotResponseData(typeof(OneBotSendForwardMessageRequest))]
internal class OneBotSendForwardMessageResponseData : IOneBotResponseData
{
    [JsonPropertyName("resid")] public required string ResId { get; set; }

    public Response ToResponse(OneBotResponse response, OneBotMessageConverter _)
        => new SendForwardMessageResponse(response.Status is not "failed", response.ReturnCode, null, ResId);
}