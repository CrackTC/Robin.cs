using Robin.Abstractions.Message;

namespace Robin.Abstractions.Operation.Requests;

public record SendGroupMessage(long GroupId, MessageChain Message) : SendMessage(Message);
