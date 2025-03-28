using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Abstractions.Utility;
using Robin.Extensions.Oa.Fetcher;
using Robin.Middlewares.Fluent;

namespace Robin.Extensions.Oa;

[BotFunctionInfo("oa", "JLU校务通知")]
public partial class OaFunction(FunctionContext<OaOption> context) : BotFunction<OaOption>(context), IFluentFunction
{
    private readonly OaFetcher _fetcher = context.Configuration.UseVpn
        ? new OaVpnFetcher(context.Configuration.VpnUsername!, context.Configuration.VpnPassword!)
        : new OaFetcher();

    private OaData? _oaData;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private async Task<string?> FetchPostResIdAsync(int postId, CancellationToken token)
    {
        var post = await _fetcher.FetchPostAsync(postId, token);
        var images = await Task.WhenAll(post.Images.Select(image => _fetcher.FetchBlobAsync(image, token)));
        // var attachs = await Task.WhenAll(post.Attachments.Select(attach => _fetcher.FetchBlobAsync(attach.Url, token)));
        var resp = await new SendGroupForwardMessageRequest(_context.Configuration.TempGroup, [
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
        ]).SendAsync(_context, token);

        if (resp is not { Result: { ResId: var resId } }) return null;

        return resId;
    }

    private async Task<List<CustomNodeData>> FetchNewPostsAsync(CancellationToken token)
    {
        var posts = await _fetcher.FetchPostsAsync(token);

        // we need to check all posts because newer posts may not be placed on top
        // thx to the shitty design of JLU OA
        var newPosts = posts
            .Where(post => !_oaData!.PostIds.Contains(post.Id))
            .Reverse()
            .ToList();

        _oaData = new(posts.Select(post => post.Id).ToHashSet());
        await SaveAsync(token);

        var resIds = await Task.WhenAll(newPosts.Select(post => FetchPostResIdAsync(post.Id, token)));

        return resIds
            .OfType<string>()
            .Select(id => new CustomNodeData(_context.BotContext.Uin, "robin", [new ForwardData(id)]))
            .ToList();
    }

    private async Task SaveAsync(CancellationToken token)
    {
        await using var stream = File.Create("oa.json");
        await JsonSerializer.SerializeAsync(stream, _oaData, cancellationToken: token);
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

    public override Task StopAsync(CancellationToken token)
    {
        _semaphore.Dispose();
        return Task.CompletedTask;
    }

    private async Task<IEnumerable<long>> GetGroupsAsync(CancellationToken token) => _context.GroupFilter switch
    {
        { Whitelist: true, Ids: var ids } => ids,
        { Ids: var ids } when await new GetGroupListRequest().SendAsync(_context, token)
            is { Success: true, Groups: var groups } => groups.Select(g => g.GroupId).Except(ids),
        _ => []
    };

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken _)
    {
        builder.OnCron("0 0 * * * ?")
            .Do(token => _semaphore.ConsumeAsync(async Task () =>
            {
                try
                {
                    if (await FetchNewPostsAsync(token) is { Count: > 0 } nodes)
                        await Task.WhenAll((await GetGroupsAsync(token)).Select(
                            groupId => new SendGroupForwardMessageRequest(groupId, nodes).SendAsync(_context, token)
                        ));
                }
                catch (Exception e)
                {
                    LogException(_context.Logger, e);
                }
            }, token));

        return Task.CompletedTask;
    }
}

public partial class OaFunction
{
    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Failed to fetch oa posts")]
    private static partial void LogException(ILogger logger, Exception e);
}
