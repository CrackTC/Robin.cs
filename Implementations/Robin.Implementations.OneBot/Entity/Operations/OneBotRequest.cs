using System.Text.Json.Nodes;

namespace Robin.Implementations.OneBot.Entity.Operations;

internal record OneBotRequest(string Endpoint, JsonObject Params);