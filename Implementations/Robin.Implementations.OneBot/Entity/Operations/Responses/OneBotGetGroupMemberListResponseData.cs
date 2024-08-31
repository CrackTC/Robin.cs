using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Responses;
using Robin.Implementations.OneBot.Converter;
using Robin.Implementations.OneBot.Entity.Operations.Requests;

namespace Robin.Implementations.OneBot.Entity.Operations.Responses;

[Serializable]
[OneBotResponseData(typeof(OneBotGetGroupMemberListRequest))]
internal class OneBotGetGroupMemberListResponseData : List<OneBotGetGroupMemberInfoResponseData>, IOneBotResponseData
{
    public Response ToResponse(OneBotResponse response, OneBotMessageConverter converter) =>
        new GetGroupMemberListResponse(
            response.Status is not "failed",
            response.ReturnCode,
            null,
            this.Select(data => (data.ToResponse(response, converter) as GetGroupMemberInfoResponse)!.Info!).ToList()
        );
}