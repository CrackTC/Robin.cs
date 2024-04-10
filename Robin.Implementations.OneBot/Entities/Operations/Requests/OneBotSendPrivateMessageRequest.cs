using System.Text.Json;
using System.Text.Json.Nodes;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Entities.Operations.Requests;

[OneBotRequest("send_private_msg", typeof(SendPrivateMessageRequest))]
internal class OneBotSendPrivateMessageRequest : IOneBotRequest
{
    public JsonNode? GetJson(Request request, OneBotMessageConverter converter)
    {
        if (request is not SendPrivateMessageRequest r) return null;

        return JsonSerializer.SerializeToNode(new
        {
            user_id = r.UserId,
            message = converter.SerializeToArray(r.Message)
        });
    }
}