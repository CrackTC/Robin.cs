namespace Robin.Abstractions.Message.Entities;

public record CustomNodeData(long Sender, string Name, MessageChain Content) : SegmentData;