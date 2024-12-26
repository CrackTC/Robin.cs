using System.Text.Json;
using System.Text.Json.Nodes;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Operations.Requests;

using RequestType = DeleteMessageRequest;

[OneBotRequest("delete_msg", typeof(RequestType))]
internal class OneBotDeleteMessageRequest : IOneBotRequest
{
    public JsonNode? GetJson(Request request, OneBotMessageConverter _)
    {
        if (request is not RequestType r) return null;
        return JsonSerializer.SerializeToNode(new
        {
            message_id = Convert.ToInt32(r.MessageId)
        });
    }
}
