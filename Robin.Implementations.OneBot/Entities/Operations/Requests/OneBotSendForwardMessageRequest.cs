using System.Text.Json;
using System.Text.Json.Nodes;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Entities.Operations.Requests;

[OneBotRequest("send_forward_msg", typeof(SendForwardMessageRequest))]
internal class OneBotSendForwardMessageRequest : IOneBotRequest
{
    public JsonNode? GetJson(Request request, OneBotMessageConverter converter)
    {
        if (request is not SendForwardMessageRequest r) return null;

        return JsonSerializer.SerializeToNode(new
        {
            messages = converter.SerializeToArray([.. r.Messages])
        });
    }
}