using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Abstractions.Operation.Responses;
using Robin.Middlewares.Fluent;
using Robin.Middlewares.Fluent.Event;

namespace Robin.Extensions.Wife;

[BotFunctionInfo("wife", "今日老婆")]
public partial class WifeFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken token)
    {
        builder.On<GroupMessageEvent>()
            .OnCommand("今日老婆", string.Empty)
            .Do(async ctx =>
            {
                var (e, t) = ctx;

                if (await new GetGroupMemberListRequest(e.GroupId).SendAsync(_context, t)
                    is not { Members: { } members })
                    return;

                var member = members[Random.Shared.Next(members.Count)];
                while (member.UserId == e.UserId)
                    member = members[Random.Shared.Next(members.Count)];

                await e.NewMessageRequest([
                    new ReplyData(e.MessageId),
                    new TextData($"今天的老婆是：{member.Card switch
                    {
                        null or "" => member.Nickname,
                        _ => member.Card
                    }}"),
                    new ImageData($"https://q1.qlogo.cn/g?b=qq&nk={member.UserId}&s=640"),
                ]).SendAsync(_context, t);
            });

        return Task.CompletedTask;
    }
}
