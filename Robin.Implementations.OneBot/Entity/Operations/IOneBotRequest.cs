using System.Text.Json.Nodes;
using Robin.Abstractions.Operation;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Operations;

internal interface IOneBotRequest
{
    JsonNode? GetJson(Request request, OneBotMessageConverter converter);
}