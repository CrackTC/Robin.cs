using Robin.Abstractions.Common;

namespace Robin.Abstractions.Message;

public record MessageChain(EquatableImmutableArray<SegmentData> Segments);