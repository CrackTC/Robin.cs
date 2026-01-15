using System.Text.Json;
using System.Text.Json.Serialization;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Message.Data;

[Serializable]
[OneBotSegmentData("poke", typeof(PokeData))]
internal class OneBotPokeData : IOneBotSegmentData
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    public SegmentData ToSegmentData(OneBotMessageConverter _) =>
        new PokeData(Convert.ToInt32(Type), Convert.ToInt32(Id), Name);

    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter)
    {
        var d = data as PokeData;
        Type = d!.Type.ToString();
        Id = d.Id.ToString();
        Name = d.Name;
        return new OneBotSegment { Type = "poke", Data = JsonSerializer.SerializeToNode(this) };
    }
}
