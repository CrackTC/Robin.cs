using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Request;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Middlewares.Fluent;

namespace Robin.Extensions.Approve;

[BotFunctionInfo("approve", "approve requests")]
public class ApproveFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken _)
    {
        builder
            .On<GroupInviteRequestEvent>("approve group invite")
            .Do(t => new SetGroupAddRequestRequest(t.Event.Flag, "invite", true, null).SendAsync(_context, t.Token))
            .On<FriendRequestEvent>("approve friend request")
            .Do(t => new SetFriendAddRequestRequest(t.Event.Flag, true, null).SendAsync(_context, t.Token));

        return Task.CompletedTask;
    }
}
