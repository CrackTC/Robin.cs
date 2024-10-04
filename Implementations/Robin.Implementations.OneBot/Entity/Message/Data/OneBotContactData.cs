using System.Text.Json;
using System.Text.Json.Serialization;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Message.Data;

[Serializable]
[OneBotSegmentData("contact", typeof(FriendContactData), typeof(GroupContactData))]
internal class OneBotContactData : IOneBotSegmentData
{
    [JsonPropertyName("type")] public required string Type { get; set; }
    [JsonPropertyName("id")] public required string Id { get; set; }

    public SegmentData ToSegmentData(OneBotMessageConverter _) =>
        Type switch
        {
            "qq" => new FriendContactData(Convert.ToInt64(Id)),
            _ => new GroupContactData(Convert.ToInt64(Id))
        };

    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter)
    {
        switch (data)
        {
            case FriendContactData d:
                Type = "qq";
                Id = d.Uin.ToString();
                return new OneBotSegment { Type = "contact", Data = JsonSerializer.SerializeToNode(this) };
            case GroupContactData d:
                Type = "group";
                Id = d.Uin.ToString();
                return new OneBotSegment { Type = "contact", Data = JsonSerializer.SerializeToNode(this) };
            default:
                throw new Exception();
        }
    }
}
