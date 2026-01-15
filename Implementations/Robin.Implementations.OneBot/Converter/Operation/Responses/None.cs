using System.Text.Json;
using Robin.Abstractions.Operation;
using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Responses;

internal class None : IOneBotResponseConverter<Response>
{
    public async Task<Response> ConvertFromResponseStream(
        Stream respStream,
        OneBotMessageConverter converter,
        CancellationToken token
    )
    {
        if (
            await JsonSerializer.DeserializeAsync<OneBotResponse<object>>(
                respStream,
                cancellationToken: token
            )
            is not { ReturnCode: 0 }
        )
            throw new();

        return new();
    }
}
