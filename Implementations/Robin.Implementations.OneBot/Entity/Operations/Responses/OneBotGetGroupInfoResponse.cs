using Robin.Abstractions.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Responses;
using Robin.Implementations.OneBot.Converter;
using Robin.Implementations.OneBot.Entity.Common;
using Robin.Implementations.OneBot.Entity.Operations.Requests;

namespace Robin.Implementations.OneBot.Entity.Operations.Responses;

[Serializable]
[OneBotResponseData(typeof(OneBotGetGroupInfoRequest))]
internal class OneBotGetGroupInfoResponseData : OneBotGroupInfo, IOneBotResponseData
{
    public Response ToResponse(OneBotResponse response, OneBotMessageConverter _) =>
        new GetGroupInfoResponse(
            response.Status is not "failed",
            response.ReturnCode,
            null,
            new GroupInfo(
                GroupId,
                GroupName,
                MemberCount,
                MaxMemberCount
            )
        );
}
