using Robin.Abstractions.Operation;
using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation;

internal interface IOneBotRequestConverter
{
    OneBotRequest ConvertToOneBotRequest(Request request, OneBotMessageConverter converter);
}

internal abstract class OneBotRequestConverter<TReq> : IOneBotRequestConverter
    where TReq : Request
{
    public abstract OneBotRequest ConvertToOneBotRequest(
        TReq request,
        OneBotMessageConverter converter
    );

    public OneBotRequest ConvertToOneBotRequest(
        Request request,
        OneBotMessageConverter converter
    ) => ConvertToOneBotRequest((TReq)request, converter);
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
