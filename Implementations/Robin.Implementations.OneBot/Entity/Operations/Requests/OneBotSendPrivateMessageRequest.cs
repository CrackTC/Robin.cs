using System.Text.Json;
using System.Text.Json.Nodes;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Operations.Requests;

using RequestType = SendPrivateMessageRequest;

[OneBotRequest("send_private_msg", typeof(RequestType))]
internal class OneBotSendPrivateMessageRequest : IOneBotRequest
{
    public JsonNode? GetJson(Request request, OneBotMessageConverter converter)
    {
        if (request is not RequestType r) return null;

        return JsonSerializer.SerializeToNode(new
        {
            user_id = r.UserId,
            message = converter.SerializeToArray(r.Message)
        });
    }
}
