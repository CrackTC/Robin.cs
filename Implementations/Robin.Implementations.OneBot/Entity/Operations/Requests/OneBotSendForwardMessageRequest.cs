using System.Text.Json;
using System.Text.Json.Nodes;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Operations.Requests;

using RequestType = SendForwardMessageRequest;

[OneBotRequest("send_forward_msg", typeof(RequestType))]
internal class OneBotSendForwardMessageRequest : IOneBotRequest
{
    public JsonNode? GetJson(Request request, OneBotMessageConverter converter)
    {
        if (request is not RequestType r) return null;

        return JsonSerializer.SerializeToNode(new
        {
            messages = converter.SerializeToArray([.. r.Messages])
        });
    }
}
