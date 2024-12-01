using System.Text.Json;
using System.Text.Json.Nodes;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Operations.Requests;

using RequestType = SetGroupReactionRequest;

[OneBotRequest("set_group_reaction", typeof(RequestType))]
internal class OneBotSetGroupReactionRequest : IOneBotRequest
{
    public JsonNode? GetJson(Request request, OneBotMessageConverter _)
    {
        if (request is not RequestType r) return null;

        return JsonSerializer.SerializeToNode(new
        {
            group_id = r.GroupId,
            message_id = long.Parse(r.MessageId),
            code = r.Code,
            is_add = r.IsAdd
        });
    }
}
