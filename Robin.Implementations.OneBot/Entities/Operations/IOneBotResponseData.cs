using Robin.Abstractions.Operation;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Entities.Operations;

internal interface IOneBotResponseData
{
    Response ToResponse(OneBotResponse response, OneBotMessageConverter converter);
}