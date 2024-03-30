using System.Text.Json;
using System.Text.Json.Nodes;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Entities.Operations.Requests;

[OneBotRequest("send_group_msg", typeof(SendGroupMessageRequest))]
internal class OneBotSendGroupMessageRequest : IOneBotRequest
{
    public JsonNode? GetJson(Request request, OneBotMessageConverter converter)
    {
        if (request is not SendGroupMessageRequest r) return null;

        return JsonSerializer.SerializeToNode(new
        {
            group_id = r.GroupId,
            message = converter.SerializeToArray(r.Message),
        });
    }
}