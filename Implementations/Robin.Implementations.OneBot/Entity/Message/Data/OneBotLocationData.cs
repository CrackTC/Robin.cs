using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Message.Data;

[OneBotSegmentData("location", typeof(LocationData))]
internal class OneBotLocationData : IOneBotSegmentData
{
    [JsonPropertyName("lat")]
    public required string Lat { get; set; }

    [JsonPropertyName("lon")]
    public required string Lon { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    public SegmentData ToSegmentData(OneBotMessageConverter _) =>
        new LocationData(Convert.ToDouble(Lat), Convert.ToDouble(Lon), Title, Content);

    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter)
    {
        var d = data as LocationData;
        Lat = d!.Latitude.ToString(CultureInfo.InvariantCulture);
        Lon = d.Longitude.ToString(CultureInfo.InvariantCulture);
        Title = d.Title;
        Content = d.Description;
        return new OneBotSegment { Type = "location", Data = JsonSerializer.SerializeToNode(this) };
    }
}
