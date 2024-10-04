namespace Robin.Abstractions.Message.Entity;

public record CustomNodeData(long Sender, string Name, MessageChain Content) : SegmentData;
