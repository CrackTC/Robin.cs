using Robin.Abstractions.Message;

namespace Robin.Abstractions.Operation.Requests;

public record SendPrivateMessage(long UserId, MessageChain Message) : SendMessage(Message);
