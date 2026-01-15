using System.Text.Json;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Utility;
using Robin.Middlewares.Fluent;
using Robin.Middlewares.Fluent.Event;

namespace Robin.Extensions.WhoAtMe;

using Data = Dictionary<long, Dictionary<long, string>>;

[BotFunctionInfo("whoatme", "谁@我")]
public class WhoAtMeFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    private Data? _latestAt;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public override async Task StartAsync(CancellationToken token)
    {
        if (File.Exists("whoatme.json"))
        {
            await using var stream = File.OpenRead("whoatme.json");
            _latestAt = await JsonSerializer.DeserializeAsync<Data>(stream, cancellationToken: token);
        }
        else
        {
            _latestAt = [];
        }
    }

    public override Task StopAsync(CancellationToken token)
    {
        _semaphore.Dispose();
        return Task.CompletedTask;
    }

    private async Task SaveAsync(CancellationToken token)
    {
        await using var stream = File.Create("whoatme.json");
        await JsonSerializer.SerializeAsync(stream, _latestAt, cancellationToken: token);
    }

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken _)
    {
        builder.On<GroupMessageEvent>("collect @")
            .OnAt()
            .AsIntrinsic()
            .Do(tuple => _semaphore.ConsumeAsync(async Task () =>
            {
                var (e, t) = tuple;
                if (!_latestAt!.ContainsKey(e.GroupId)) _latestAt[e.GroupId] = [];
                var targets = e.Message.OfType<AtData>().Select(at => at.Uin);
                foreach (var target in targets)
                    _latestAt[e.GroupId][target] = e.MessageId;

                await SaveAsync(t);
            }, tuple.Token))
            .On<GroupMessageEvent>("show who @ me")
            .OnCommand("谁@我", prefix: string.Empty)
            .Do(tuple => _semaphore.ConsumeAsync(async Task () =>
            {
                var (e, t) = tuple;
                if (!_latestAt!.TryGetValue(e.GroupId, out var ats) || !ats.ContainsKey(e.Sender.UserId))
                {
                    await e.NewMessageRequest([
                        new ReplyData(e.MessageId),
                        new TextData("暂时没有新的@喵")
                    ]).SendAsync(_context, t);
                    return;
                }

                await e.NewMessageRequest([new ReplyData(_latestAt[e.GroupId][e.Sender.UserId]), new TextData("这里这里")]).SendAsync(_context, t);

                _latestAt[e.GroupId].Remove(e.Sender.UserId);
                await SaveAsync(t);
            }, tuple.Token));
        return Task.CompletedTask;
    }
}
