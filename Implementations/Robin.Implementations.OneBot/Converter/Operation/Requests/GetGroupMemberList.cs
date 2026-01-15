using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Requests;

internal class GetGroupMemberList
    : OneBotRequestConverter<Abstractions.Operation.Requests.GetGroupMemberList>
{
    public override OneBotRequest ConvertToOneBotRequest(
        Abstractions.Operation.Requests.GetGroupMemberList request,
        OneBotMessageConverter _
    ) =>
        new(
            "get_group_member_list",
            new() { ["group_id"] = request.GroupId, ["no_cache"] = request.NoCache }
        );
}
