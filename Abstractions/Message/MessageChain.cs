using Robin.Common;

namespace Robin.Abstractions.Message;

public record MessageChain(EquatableImmutableArray<SegmentData> Segments);