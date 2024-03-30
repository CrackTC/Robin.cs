using System.Collections.Immutable;
using Robin.Abstractions.Common;
using Robin.Abstractions.Entities;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Responses;
using Robin.Implementations.OneBot.Entities.Operations.Requests;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Entities.Operations.Responses;

[Serializable]
[OneBotResponseData(typeof(OneBotGetGroupMemberListRequest))]
internal class OneBotGetGroupMemberListResponseData : List<OneBotGetGroupMemberInfoResponseData>, IOneBotResponseData
{
    public Response ToResponse(OneBotResponse response, OneBotMessageConverter converter) =>
        new GetGroupMemberListResponse(
            response.Status is not "failed",
            response.ReturnCode,
            null,
            new EquatableImmutableArray<GroupMemberInfo>(
                this.Select(data => (data.ToResponse(response, converter) as GetGroupMemberInfoResponse)!.Info!)
                    .ToImmutableArray()
            )
        );
}