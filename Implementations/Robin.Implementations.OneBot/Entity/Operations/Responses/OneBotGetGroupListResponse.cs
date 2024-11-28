using Robin.Abstractions.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Responses;
using Robin.Implementations.OneBot.Converter;
using Robin.Implementations.OneBot.Entity.Common;
using Robin.Implementations.OneBot.Entity.Operations.Requests;

namespace Robin.Implementations.OneBot.Entity.Operations.Responses;

using OneBotRequestType = OneBotGetGroupListRequest;
using ResponseType = GetGroupListResponse;

[Serializable]
[OneBotResponseData(typeof(OneBotRequestType))]
internal class OneBotGetGroupListResponseData : List<OneBotGroupInfo>, IOneBotResponseData
{
    public Response ToResponse(OneBotResponse response, OneBotMessageConverter _) =>
        new ResponseType(
            response.Status is not "failed",
            response.ReturnCode,
            null,
            this.Select(info => new GroupInfo(info.GroupId, info.GroupName, info.MemberCount, info.MaxMemberCount)).ToList()
        );
}
