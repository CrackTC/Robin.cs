using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Requests;

internal class SetGroupReaction : IOneBotRequestConverter<Abstractions.Operation.Requests.SetGroupReaction>
{
    public OneBotRequest ConvertToOneBotRequest(Abstractions.Operation.Requests.SetGroupReaction request, OneBotMessageConverter _) =>
        new("set_group_reaction", new()
        {
            ["group_id"] = request.GroupId,
            ["message_id"] = Convert.ToInt64(request.MessageId),
            ["code"] = request.Code,
            ["is_add"] = request.IsAdd
        });
}
