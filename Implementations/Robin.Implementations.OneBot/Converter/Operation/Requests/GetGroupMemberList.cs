using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Requests;

internal class GetGroupMemberList
    : IOneBotRequestConverter<Abstractions.Operation.Requests.GetGroupMemberList>
{
    public OneBotRequest ConvertToOneBotRequest(
        Abstractions.Operation.Requests.GetGroupMemberList request,
        OneBotMessageConverter _
    ) =>
        new(
            "get_group_member_list",
            new() { ["group_id"] = request.GroupId, ["no_cache"] = request.NoCache }
        );
}
