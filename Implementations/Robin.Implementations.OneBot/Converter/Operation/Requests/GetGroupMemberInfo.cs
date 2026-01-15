using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Requests;

internal class GetGroupMemberInfo
    : OneBotRequestConverter<Abstractions.Operation.Requests.GetGroupMemberInfo>
{
    public override OneBotRequest ConvertToOneBotRequest(
        Abstractions.Operation.Requests.GetGroupMemberInfo request,
        OneBotMessageConverter _
    ) =>
        new(
            "get_group_member_info",
            new()
            {
                ["group_id"] = request.GroupId,
                ["user_id"] = request.UserId,
                ["no_cache"] = request.NoCache,
            }
        );
}
