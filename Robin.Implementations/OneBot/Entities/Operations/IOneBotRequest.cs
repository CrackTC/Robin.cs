using System.Text.Json.Nodes;
using Robin.Abstractions.Operation;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Entities.Operations;

internal interface IOneBotRequest
{
    JsonNode? GetJson(Request request, OneBotMessageConverter converter);
}