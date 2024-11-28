using System.Text.Json;
using System.Text.Json.Nodes;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Operations.Requests;

using RequestType = SetGroupAddRequestRequest;

[OneBotRequest("set_group_add_request", typeof(RequestType))]
internal class OneBotSetGroupAddRequestRequest : IOneBotRequest
{
    public JsonNode? GetJson(Request request, OneBotMessageConverter _)
    {
        if (request is not RequestType r) return null;

        return JsonSerializer.SerializeToNode(new
        {
            flag = r.Flag,
            sub_type = r.SubType,
            approve = r.Approve,
            reason = r.Reason
        });
    }
}
