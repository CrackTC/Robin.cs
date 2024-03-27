using System.Collections.Immutable;

namespace Robin.Abstractions.Message;

public class MessageBuilder
{
    private readonly List<SegmentData> _segments = [];

    public MessageBuilder Add(SegmentData data)
    {
        _segments.Add(data);
        return this;
    }

    public MessageChain Build() => new(_segments.ToImmutableArray());
}