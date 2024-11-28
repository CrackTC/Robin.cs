using System.Text.Json;
using System.Text.Json.Nodes;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Operations.Requests;

using RequestType = SendGroupForwardMessageRequest;

[OneBotRequest("send_group_forward_msg", typeof(RequestType))]
internal class OneBotSendGroupForwardMessageRequest : IOneBotRequest
{
    public JsonNode? GetJson(Request request, OneBotMessageConverter converter)
    {
        if (request is not RequestType r) return null;

        return JsonSerializer.SerializeToNode(new
        {
            group_id = r.GroupId,
            messages = converter.SerializeToArray([.. r.Messages])
        });
    }
}
