using System.Text.Json;
using System.Text.Json.Serialization;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Responses;
using Robin.Implementations.OneBot.Converter;
using Robin.Implementations.OneBot.Entity.Operations.Requests;

namespace Robin.Implementations.OneBot.Entity.Operations.Responses;

using OneBotRequestType = OneBotSendForwardMessageRequest;
using ResponseType = SendForwardMessageResponse;

[Serializable]
[OneBotResponseData(typeof(OneBotRequestType))]
[JsonConverter(typeof(StringToResponseDataConverter))]
internal class OneBotSendForwardMessageResponseData : IOneBotResponseData
{
    [JsonPropertyName("resid")] public required string ResId { get; set; }

    public Response ToResponse(OneBotResponse response, OneBotMessageConverter _)
        => new ResponseType(response.Status is not "failed", response.ReturnCode, null, ResId);
}

file class StringToResponseDataConverter : JsonConverter<OneBotSendForwardMessageResponseData>
{
    public override OneBotSendForwardMessageResponseData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String) throw new JsonException();
        return new OneBotSendForwardMessageResponseData { ResId = reader.GetString()! };
    }

    public override void Write(Utf8JsonWriter writer, OneBotSendForwardMessageResponseData value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
