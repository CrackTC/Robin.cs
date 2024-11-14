using System.Security.Cryptography;
using System.Text.Json;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Abstractions.Utility;
using Robin.Extensions.Oa.Entity;
using Robin.Extensions.Oa.Fetcher;
using Robin.Middlewares.Annotations.Cron;
using Robin.Middlewares.Fluent;
using Robin.Middlewares.Fluent.Event;

namespace Robin.Extensions.Oa;

[BotFunctionInfo("oa", "JLU校务通知")]
[OnCron("0 0 * * * ?")]
public class OaFunction(FunctionContext<OaOption> context) : BotFunction<OaOption>(context), IFluentFunction, ICronHandler
{
    private readonly OaFetcher _fetcher = context.Configuration.UseVpn
        ? new OaVpnFetcher(context.Configuration.VpnUsername!, context.Configuration.VpnPassword!)
        : new OaFetcher();

    private OaData? _oaData;
    private readonly RingBuffer<(int PostId, Lazy<Task<List<CustomNodeData>>> Nodes)> _normalPostBuffer = new(30);
    private readonly RingBuffer<(int PostId, Lazy<Task<List<CustomNodeData>>> Nodes)> _pinnedPostBuffer = new(30);
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private async Task<List<CustomNodeData>> GetPostNodes(int postId, CancellationToken token)
    {
        var post = await _fetcher.FetchPostAsync(postId, token);
        var images = await Task.WhenAll(post.Images.Select(image => _fetcher.FetchBlobAsync(image, token)));
        // var attachs = await Task.WhenAll(post.Attachments.Select(attach => _fetcher.FetchBlobAsync(attach.Url, token)));
        return [
            new(_context.BotContext.Uin, "robin", [
                new TextData(
                    $"""
                    标题：{post.Title}
                    发布时间：{post.DateTime}
                    来源：{post.Source}
                    """
                )
            ]),
            new(_context.BotContext.Uin, "robin", [ new TextData(post.Content) ]),
            ..(images.Length is 0 && post.DataImages.Count is 0 ? Enumerable.Empty<CustomNodeData>() : [
                new(_context.BotContext.Uin, "robin", [
                    ..images.Select(image => {
                        using var stream = image;
                        using var b64Stream = new CryptoStream(stream, new ToBase64Transform(), CryptoStreamMode.Read);
                        using var streamReader = new StreamReader(b64Stream);
                        return new ImageData($"base64://{streamReader.ReadToEnd()}");
                    }),
                    ..post.DataImages.Select(image => new ImageData($"base64://{image[(image.IndexOf(',') + 1)..]}"))
                ])
            ]),
            ..post.Attachments.Select(attach => new CustomNodeData(_context.BotContext.Uin, "robin", [
                new TextData(
                    $"""
                    附件：{attach.Name}
                    链接：{attach.Url}
                    """
                )
            ]))
        ];
    }

    private async Task UpdateOaPosts(CancellationToken token)
    {
        var postIds = await _fetcher.FetchPostsAsync(token);

        // post id may not in time order, so we iterate until the last post in buffer
        foreach (var (_, id) in postIds
            .Where(t => t.Pinned)
            .TakeWhile(t => t.Id != _pinnedPostBuffer.Last?.PostId)
            .Reverse())
            _pinnedPostBuffer.Add((id, new(() => GetPostNodes(id, token))));

        foreach (var (_, id) in postIds
            .Where(t => !t.Pinned)
            .TakeWhile(t => t.Id != _normalPostBuffer.Last?.PostId)
            .Reverse())
            _normalPostBuffer.Add((id, new(() => GetPostNodes(id, token))));
    }

    private async Task<int> SendPostsToGroup(long groupId, CancellationToken token)
    {
        if (!_oaData!.Groups.ContainsKey(groupId))
            _oaData!.Groups.Add(groupId, new(LastPinnedPostId: 0, LastNormalPostId: 0));
        var group = _oaData!.Groups[groupId];

        var newerPinned = _pinnedPostBuffer.GetItems().SkipWhile(p => p.PostId != group.LastPinnedPostId).ToList();
        var newerNormal = _normalPostBuffer.GetItems().SkipWhile(p => p.PostId != group.LastNormalPostId).ToList();

        int count = 0;

        foreach (var (_, nodes) in newerPinned.Count is 0
            ? _pinnedPostBuffer.GetItems()
            : newerPinned.Skip(1))
        {
            await new SendGroupForwardMessageRequest(groupId, await nodes.Value).SendAsync(_context, token);
            ++count;
        }
        foreach (var (_, nodes) in newerNormal.Count is 0
            ? _normalPostBuffer.GetItems()
            : newerNormal.Skip(1))
        {
            await new SendGroupForwardMessageRequest(groupId, await nodes.Value).SendAsync(_context, token);
            ++count;
        }

        _oaData!.Groups[groupId] = new(
            _pinnedPostBuffer.Last?.PostId ?? 0,
            _normalPostBuffer.Last?.PostId ?? 0
        );

        return count;
    }

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken token)
    {
        builder.On<GroupMessageEvent>()
            .OnCommand("oa")
            .Where(tuple => _context.Configuration.Groups!.Contains(tuple.Event.GroupId))
            .Do(tuple => _semaphore.ConsumeAsync(async Task () =>
            {
                var (e, t) = tuple;
                await UpdateOaPosts(t);
                if (await SendPostsToGroup(e.GroupId, t) is 0)
                    await e.NewMessageRequest([new TextData("没有新通知喵>_<")]).SendAsync(_context, t);
                await SaveAsync(t);
            }, tuple.Token));

        return Task.CompletedTask;
    }

    public Task OnCronEventAsync(CancellationToken token) => _semaphore.ConsumeAsync(async Task () =>
    {
        await UpdateOaPosts(token);
        await Task.WhenAll(_context.Configuration.Groups!.Select(groupId => SendPostsToGroup(groupId, token)));
        await SaveAsync(token);
    }, token);

    private async Task SaveAsync(CancellationToken token)
    {
        if (_oaData is not null)
        {
            await using var stream = File.Create("oa.json");
            await JsonSerializer.SerializeAsync(stream, _oaData, cancellationToken: token);
        }
    }

    public override async Task StartAsync(CancellationToken token)
    {
        if (File.Exists("oa.json"))
        {
            await using var stream = File.OpenRead("oa.json");
            _oaData = await JsonSerializer.DeserializeAsync<OaData>(stream, cancellationToken: token);
        }
        else
        {
            _oaData = new OaData([]);
        }
    }

    public override async Task StopAsync(CancellationToken token)
    {
        _semaphore.Dispose();
        await SaveAsync(token);
    }
}
