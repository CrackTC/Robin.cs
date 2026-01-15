using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Requests;

[OneBotVariant("napcat")]
internal class SetMessageEmojiLike
    : OneBotRequestConverter<Abstractions.Operation.Requests.SetGroupReaction>
{
    public override OneBotRequest ConvertToOneBotRequest(
        Abstractions.Operation.Requests.SetGroupReaction request,
        OneBotMessageConverter _
    ) =>
        new(
            "set_msg_emoji_like",
            new()
            {
                ["message_id"] = request.MessageId,
                ["emoji_id"] = request.Code,
                ["set"] = request.IsAdd,
            }
        );
}
