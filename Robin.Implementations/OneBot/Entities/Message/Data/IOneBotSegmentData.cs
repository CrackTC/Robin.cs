using Robin.Abstractions.Message;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Entities.Message.Data;

internal interface IOneBotSegmentData
{
    SegmentData ToSegmentData(OneBotMessageConverter converter);
    OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter);
}