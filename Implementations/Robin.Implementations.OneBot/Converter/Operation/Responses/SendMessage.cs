using System.Text.Json;
using Robin.Abstractions.Operation.Responses;
using Robin.Implementations.OneBot.Entity.Common;
using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Responses;

internal class SendMessage : IOneBotResponseConverter<SendMessageResponse>
{
    public async Task<SendMessageResponse> ConvertFromResponseStream(Stream respStream, OneBotMessageConverter _, CancellationToken token)
    {
        if (await JsonSerializer.DeserializeAsync<OneBotResponse<OneBotMessageId>>(respStream, cancellationToken: token)
            is not { Data: { } data }) throw new();

        return new(data.ToMessageId());
    }
}
