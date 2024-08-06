using Robin.Abstractions.Operation;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Operations;

internal interface IOneBotResponseData
{
    Response ToResponse(OneBotResponse response, OneBotMessageConverter converter);
}