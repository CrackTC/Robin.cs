using System.Text.Json;
using Robin.Abstractions.Operation.Responses;
using Robin.Implementations.OneBot.Entity.Common;
using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Responses;

internal class GetGroupMemberList : IOneBotResponseConverter<GetGroupMemberListResponse>
{
    public async Task<GetGroupMemberListResponse> ConvertFromResponseStream(
        Stream respStream,
        OneBotMessageConverter _,
        CancellationToken token
    )
    {
        if (
            await JsonSerializer.DeserializeAsync<OneBotResponse<List<OneBotGroupMemberInfo>>>(
                respStream,
                cancellationToken: token
            )
            is not { Data: { } data }
        )
            throw new();

        return new([.. data.Select(info => info.ToGroupMemberInfo())]);
    }
}
