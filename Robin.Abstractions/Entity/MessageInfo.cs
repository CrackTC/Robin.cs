using Robin.Abstractions.Message;

namespace Robin.Abstractions.Entity;

public record MessageInfo(long Time, string MessageType, int MessageId, int RealId, MessageSender Sender, MessageChain Message);
