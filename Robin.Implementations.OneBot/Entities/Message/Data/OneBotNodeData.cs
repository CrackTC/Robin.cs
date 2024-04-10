using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entities;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Entities.Message.Data;

[Serializable]
[OneBotSegmentData("node", typeof(NodeData), typeof(CustomNodeData))]
internal class OneBotNodeData : IOneBotSegmentData
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("name")] public required string Name { get; set; }
    [JsonPropertyName("uin")] public required string Uin { get; set; }
    [JsonPropertyName("content")] public JsonNode? Content { get; set; }

    public SegmentData ToSegmentData(OneBotMessageConverter converter) =>
        Id switch
        {
            not null => new NodeData(Id),
            _ => new CustomNodeData(Convert.ToInt64(Uin), Name, converter.ParseMessageChain(Content) ?? [])
        };

    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter)
    {
        switch (data)
        {
            case NodeData d:
                Id = d.Id;
                return new OneBotSegment { Type = "node", Data = JsonSerializer.SerializeToNode(this) };
            case CustomNodeData d:
                Uin = d.Sender.ToString();
                Name = d.Name;
                Content = converter.SerializeToArray(d.Content);
                return new OneBotSegment { Type = "node", Data = JsonSerializer.SerializeToNode(this) };
            default:
                throw new Exception();
        }
    }
}