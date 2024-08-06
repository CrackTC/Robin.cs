using Robin.Abstractions.Message;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Message.Data;

internal interface IOneBotSegmentData
{
    SegmentData ToSegmentData(OneBotMessageConverter converter);
    OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter);
}