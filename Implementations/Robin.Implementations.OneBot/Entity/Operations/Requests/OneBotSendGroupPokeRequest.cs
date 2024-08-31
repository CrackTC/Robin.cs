using System.Text.Json;
using System.Text.Json.Nodes;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Operations.Requests;

[OneBotRequest("group_poke", typeof(SendGroupPokeRequest))]
internal class OneBotSendGroupPokeRequest : IOneBotRequest
{
    public JsonNode? GetJson(Request request, OneBotMessageConverter _)
    {
        if (request is not SendGroupPokeRequest r) return null;

        return JsonSerializer.SerializeToNode(new
        {
            group_id = r.GroupId,
            user_id = r.UserId
        });
    }
}