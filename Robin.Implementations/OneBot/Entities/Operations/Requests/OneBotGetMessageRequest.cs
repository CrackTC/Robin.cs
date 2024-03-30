using System.Text.Json;
using System.Text.Json.Nodes;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Entities.Operations.Requests;

[OneBotRequest("get_msg", typeof(GetMessageRequest))]
internal class OneBotGetMessageRequest : IOneBotRequest
{
    public JsonNode? GetJson(Request request, OneBotMessageConverter _)
    {
        if (request is not GetMessageRequest r) return null;
        return JsonSerializer.SerializeToNode(new
        {
            message_id = r.MessageId
        });
    }
}