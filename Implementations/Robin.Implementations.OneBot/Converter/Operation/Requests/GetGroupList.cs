using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Requests;

internal class GetGroupList : IOneBotRequestConverter<Abstractions.Operation.Requests.GetGroupList>
{
    public OneBotRequest ConvertToOneBotRequest(
        Abstractions.Operation.Requests.GetGroupList _,
        OneBotMessageConverter __
    ) => new("get_group_list", []);
}
