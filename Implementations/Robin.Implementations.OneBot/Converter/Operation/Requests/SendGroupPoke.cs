using Robin.Abstractions.Operation.Requests;
using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Requests;

internal class GroupPoke : OneBotRequestConverter<SendGroupPoke>
{
    public override OneBotRequest ConvertToOneBotRequest(
        SendGroupPoke request,
        OneBotMessageConverter _
    ) => new("group_poke", new() { ["group_id"] = request.GroupId, ["user_id"] = request.UserId });
}
