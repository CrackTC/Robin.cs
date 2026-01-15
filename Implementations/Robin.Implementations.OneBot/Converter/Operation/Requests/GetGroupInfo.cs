using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Requests;

internal class GetGroupInfo : IOneBotRequestConverter<Abstractions.Operation.Requests.GetGroupInfo>
{
    public OneBotRequest ConvertToOneBotRequest(
        Abstractions.Operation.Requests.GetGroupInfo request,
        OneBotMessageConverter _
    ) =>
        new(
            "get_group_info",
            new() { ["group_id"] = request.GroupId, ["no_cache"] = request.NoCache }
        );
}
