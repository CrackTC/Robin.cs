using Robin.Abstractions.Message;

namespace Robin.Abstractions.Entity;

public record MessageInfo(long Time, MessageType MessageType, string MessageId, string RealId, MessageSender Sender, MessageChain Message);
