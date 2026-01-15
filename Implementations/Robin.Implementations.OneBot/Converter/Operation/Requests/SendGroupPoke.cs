using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Requests;

internal class SendGroupPoke : IOneBotRequestConverter<Abstractions.Operation.Requests.SendGroupPoke>
{
    public OneBotRequest ConvertToOneBotRequest(Abstractions.Operation.Requests.SendGroupPoke request, OneBotMessageConverter _) =>
        new("group_poke", new()
        {
            ["group_id"] = request.GroupId,
            ["user_id"] = request.UserId
        });
}
