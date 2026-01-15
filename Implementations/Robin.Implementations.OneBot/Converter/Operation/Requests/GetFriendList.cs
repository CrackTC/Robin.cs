using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Requests;

internal class GetFriendList
    : IOneBotRequestConverter<Abstractions.Operation.Requests.GetFriendList>
{
    public OneBotRequest ConvertToOneBotRequest(
        Abstractions.Operation.Requests.GetFriendList _,
        OneBotMessageConverter __
    ) => new("get_friend_list", []);
}
