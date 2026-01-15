using System.Text.Json.Serialization;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Notice.Reaction;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Events.Notice;

internal class NapCatGroupMsgEmojiLike
{
    [JsonPropertyName("emoji_id")]
    public required string EmojiId { get; set; }

    [JsonPropertyName("count")]
    public required uint Count { get; set; }
}

[OneBotEventType("group_msg_emoji_like")]
internal class NapCatGroupMsgEmojiLikeEvent : OneBotNoticeEvent
{
    [JsonPropertyName("group_id")]
    public required long GroupId { get; set; }

    [JsonPropertyName("user_id")]
    public required long UserId { get; set; }

    [JsonPropertyName("message_id")]
    public required long MessageId { get; set; }

    [JsonPropertyName("likes")]
    public required List<NapCatGroupMsgEmojiLike> Likes { get; set; }

    [JsonPropertyName("is_add")]
    public required bool IsAdd { get; set; }

    public override BotEvent ToBotEvent(OneBotMessageConverter converter) =>
        IsAdd switch
        {
            true => new ReactionAddEvent(
                Time,
                MessageId.ToString(),
                GroupId,
                UserId,
                Likes[0].EmojiId,
                Likes[0].Count
            ),
            _ => new ReactionRemoveEvent(
                Time,
                MessageId.ToString(),
                GroupId,
                UserId,
                Likes[0].EmojiId,
                Likes[0].Count
            ),
        };
}
