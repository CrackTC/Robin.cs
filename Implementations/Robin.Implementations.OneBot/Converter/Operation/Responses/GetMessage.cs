using System.Text.Json;
using Robin.Abstractions.Operation.Responses;
using Robin.Implementations.OneBot.Entity.Common;
using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Responses;

internal class GetMessage : IOneBotResponseConverter<GetMessageResponse>
{
    public async Task<GetMessageResponse> ConvertFromResponseStream(Stream respStream, OneBotMessageConverter converter, CancellationToken token)
    {
        if (await JsonSerializer.DeserializeAsync<OneBotResponse<OneBotMessageInfo>>(respStream, cancellationToken: token)
            is not { Data: { } data }) throw new();

        return new(data.ToMessageInfo(converter));
    }
}
