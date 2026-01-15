using Robin.Abstractions.Operation;
using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation;

internal interface IOneBotRequestConverter;

internal interface IOneBotRequestConverter<TReq> : IOneBotRequestConverter
    where TReq : Request
{
    OneBotRequest ConvertToOneBotRequest(TReq request, OneBotMessageConverter converter);
}

internal interface IOneBotResponseConverter;

internal interface IOneBotResponseConverter<TResp> : IOneBotResponseConverter
    where TResp : Response
{
    Task<TResp> ConvertFromResponseStream(
        Stream respStream,
        OneBotMessageConverter converter,
        CancellationToken token
    );
}
