using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Requests;

internal class GetGroupList : OneBotRequestConverter<Abstractions.Operation.Requests.GetGroupList>
{
    public override OneBotRequest ConvertToOneBotRequest(
        Abstractions.Operation.Requests.GetGroupList _,
        OneBotMessageConverter __
    ) => new("get_group_list", []);
}
