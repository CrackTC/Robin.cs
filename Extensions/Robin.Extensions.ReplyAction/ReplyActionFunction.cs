using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Abstractions.Operation.Responses;
using Robin.Middlewares.Fluent;
using Robin.Middlewares.Fluent.Event;
using System.Text.RegularExpressions;

namespace Robin.Extensions.ReplyAction;

[BotFunctionInfo("reply_action", "把字句制造机")]
// ReSharper disable once UnusedType.Global
public partial class ReplyActionFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    [GeneratedRegex(@"^/\S")]
    private static partial Regex IsAction();

    [GeneratedRegex(@"^/(?<verb>\S+)(?:\s+(?<adverb>.*))?")]
    private static partial Regex ActionParts();

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken _)
    {
        builder.On<GroupMessageEvent>()
            .OnRegex(IsAction())
            .Select(e => e.EventContext)
            .OnReply()
            .AsFallback()
            .Do(async t =>
            {
                var (ctx, msgId) = t;
                var (e, token) = ctx;

                var match = ActionParts().Match(
                    string.Join(
                        null,
                        e.Message.OfType<TextData>()
                            .Select(data => data.Text.Trim())
                    )
                );

                var verb = match.Groups["verb"];
                var adverb = match.Groups["adverb"];

                if (await new GetMessageRequest(msgId)
                        .SendAsync<GetMessageResponse>(_context.BotContext.OperationProvider, _context.Logger, token)
                    is not { Message.Sender.UserId: var senderId })
                    return;

                if (await new GetGroupMemberInfoRequest(e.GroupId, senderId, true)
                        .SendAsync<GetGroupMemberInfoResponse>(_context.BotContext.OperationProvider, _context.Logger, token)
                    is not { Info: { } info })
                    return;

                var sourceName = e.Sender.Card switch
                {
                    null or "" => e.Sender.Nickname,
                    _ => e.Sender.Card
                };

                var targetName = info.Card switch
                {
                    null or "" => info.Nickname,
                    _ => info.Card
                };

                await e.NewMessageRequest([
                    new TextData($"{sourceName} {verb.Value} {targetName}{(
                        adverb.Success ? ' ' + adverb.Value : string.Empty
                    )}")
                ]).SendAsync(_context.BotContext.OperationProvider, _context.Logger, token);
            });

        return Task.CompletedTask;
    }
}
