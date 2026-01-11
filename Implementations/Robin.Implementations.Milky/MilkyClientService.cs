using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Milky.Net.Client;
using Milky.Net.Model;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Entity;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Event.Notice;
using Robin.Abstractions.Event.Notice.Member.Decrease;
using Robin.Abstractions.Event.Notice.Recall;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Abstractions.Operation.Responses;

namespace Robin.Implementations.Milky;

internal partial class MilkyClientService(
    IServiceProvider service,
    MilkyClientOption options
) : BackgroundService, IBotEventInvoker, IOperationProvider
{
    private readonly ILogger<MilkyClientService> _logger =
        service.GetRequiredService<ILogger<MilkyClientService>>();

    private MilkyClient client = null!;

    private readonly MilkyJsonSerializerContext _milkyJsonSerializerContext = new(new JsonSerializerOptions()
    {
        AllowOutOfOrderMetadataProperties = true
    });

    public event Func<BotEvent, CancellationToken, Task>? OnEventAsync;

    private static UserSex ConvertSex(Sex sex) => sex switch
    {
        Sex.Male => UserSex.Male,
        Sex.Female => UserSex.Female,
        Sex.Unknown => UserSex.Unknown,
        _ => throw new ArgumentOutOfRangeException(nameof(sex))
    };

    private static GroupMemberRole ConvertRole(Role role) => role switch
    {
        Role.Owner => GroupMemberRole.Owner,
        Role.Admin => GroupMemberRole.Admin,
        Role.Member => GroupMemberRole.Member,
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };

    public async Task<TResp?> SendRequestAsync<TResp>(RequestFor<TResp> request, CancellationToken token) where TResp : Response
    {
        switch (request)
        {
            case DeleteMessageRequest deleteMsg:
                {
                    if (deleteMsg.MessageId.Split('|') is not ([var messageScene, var peerIdStr, var messageSeqStr]))
                        throw new InvalidOperationException("Invalid message ID format");

                    var peerId = Convert.ToInt64(peerIdStr);
                    var messageSeq = Convert.ToInt64(messageSeqStr);
                    switch (messageScene)
                    {
                        case "friend":
                            await client.Message.RecallPrivateMessageAsync(new(peerId, messageSeq), token);
                            return Response.OK as TResp;
                        case "group":
                            await client.Message.RecallGroupMessageAsync(new(peerId, messageSeq), token);
                            return Response.OK as TResp;
                        default:
                            throw new NotSupportedException($"Unsupported message scene: {messageScene}");
                    }
                }
            case GetFriendListRequest getFriendList:
                {
                    var result = await client.System.GetFriendListAsync(new(false), token);
                    var resp = new GetFriendListResponse(
                        true,
                        0,
                        null,
                        [.. result.Friends.Select(friend => new FriendInfo(friend.UserId, friend.Nickname, friend.Remark))]);
                    return resp as TResp;
                }
            case GetGroupInfoRequest getGroupInfo:
                {
                    var result = await client.System.GetGroupInfoAsync(new(getGroupInfo.GroupId, getGroupInfo.NoCache), token);
                    var resp = new GetGroupInfoResponse(
                        true,
                        0,
                        null,
                        new GroupInfo(result.Group.GroupId, result.Group.GroupName, result.Group.MemberCount, result.Group.MaxMemberCount)
                    );
                    return resp as TResp;
                }
            case GetGroupListRequest getGroupList:
                {
                    var result = await client.System.GetGroupListAsync(new(false), token);
                    var resp = new GetGroupListResponse(
                        true,
                        0,
                        null,
                        [.. result.Groups.Select(group => new GroupInfo(group.GroupId, group.GroupName, group.MemberCount, group.MaxMemberCount))]);
                    return resp as TResp;
                }
            case GetGroupMemberInfoRequest getGroupMemberInfo:
                {
                    var result = await client.System.GetGroupMemberInfoAsync(new(getGroupMemberInfo.GroupId, getGroupMemberInfo.UserId, getGroupMemberInfo.NoCache), token);
                    var resp = new GetGroupMemberInfoResponse(
                        true,
                        0,
                        null,
                        new GroupMemberInfo(
                            result.Member.GroupId,
                            result.Member.UserId,
                            result.Member.Nickname,
                            result.Member.Card,
                            ConvertSex(result.Member.Sex),
                            null,
                            null,
                            result.Member.JoinTime.ToUnixTimeSeconds(),
                            result.Member.LastSentTime.ToUnixTimeSeconds(),
                            result.Member.Level,
                            ConvertRole(result.Member.Role),
                            null,
                            result.Member.Title,
                            null,
                            null
                        )
                    );
                    return resp as TResp;
                }
            case GetGroupMemberListRequest getGroupMemberList:
                {
                    var result = await client.System.GetGroupMemberListAsync(new(getGroupMemberList.GroupId, getGroupMemberList.NoCache), token);
                    var resp = new GetGroupMemberListResponse(
                        true,
                        0,
                        null,
                        [.. result.Members.Select(member => new GroupMemberInfo(
                            member.GroupId,
                            member.UserId,
                            member.Nickname,
                            member.Card,
                            ConvertSex(member.Sex),
                            null,
                            null,
                            member.JoinTime.ToUnixTimeSeconds(),
                            member.LastSentTime.ToUnixTimeSeconds(),
                            member.Level,
                            ConvertRole(member.Role),
                            null,
                            member.Title,
                            null,
                            null
                        ))]);
                    return resp as TResp;
                }
            case GetMessageRequest getMsg:
                {
                    if (getMsg.MessageId.Split('|') is not ([var messageScene, var peerIdStr, var messageSeqStr]))
                        throw new InvalidOperationException("Invalid message ID format");

                    var peerId = Convert.ToInt64(peerIdStr);
                    var messageSeq = Convert.ToInt64(messageSeqStr);
                    var result = await client.RequestAsync("get_message",
                        new(GetMessageScene(messageScene), peerId, messageSeq),
                        _milkyJsonSerializerContext.GetMessageInput,
                        _milkyJsonSerializerContext.GetMessageOutput,
                        token
                    );
                    var resp = new GetMessageResponse(
                        true,
                        0,
                        null,
                        new MessageInfo(
                            result.Message.Time.ToUnixTimeSeconds(),
                            GetMessageType(result.Message),
                            GenerateMessageId(result.Message),
                            GenerateMessageId(result.Message),
                            result.Message switch
                            {
                                FriendIncomingMessage friend => ConvertSender(friend),
                                GroupIncomingMessage group => new GroupMessageSender(
                                    group.GroupMember.UserId,
                                    group.GroupMember.Nickname,
                                    group.GroupMember.Card,
                                    ConvertSex(group.GroupMember.Sex),
                                    null,
                                    null,
                                    group.GroupMember.Level,
                                    ConvertRole(group.GroupMember.Role),
                                    group.GroupMember.Title
                                ),
                                _ => throw new InvalidOperationException("Unsupported message type")
                            },
                            ConvertSegment(result.Message)
                        )
                    );
                    return resp as TResp;
                }
            case SendGroupForwardMessageRequest sendGroupForwardMsg:
                {
                    SendGroupMessageOutput result;
                    var triesRemain = options.SendMessageMaxRetry + 1;

                    while (true)
                    {
                        triesRemain--;
                        try
                        {
                            result = await client.Message.SendGroupMessageAsync(
                                new SendGroupMessageInput(
                                    sendGroupForwardMsg.GroupId,
                                    [ConvertFromCustomNodes(sendGroupForwardMsg.Messages)]
                                ),
                                token
                            );
                            break;
                        }
                        catch (MilkyException e) when (e.Message is "Internal server error")
                        {
                            if (triesRemain > 0)
                            {
                                _logger.LogSendingMessageFailedDueToInternalServerError(triesRemain);
                                continue;
                            }

                            throw;
                        }
                    }

                    return new SendGroupForwardMessageResponse(true, 0, null,
                        new ForwardResult(GenerateMessageId(MessageScene.Group, sendGroupForwardMsg.GroupId, result.MessageSeq), "")
                    ) as TResp;
                }
            case SendGroupMessageRequest sendGroupMsg:
                {
                    SendGroupMessageOutput result;
                    var triesRemain = options.SendMessageMaxRetry + 1;
                    while (true)
                    {
                        triesRemain--;
                        try
                        {
                            result = await client.Message.SendGroupMessageAsync(
                                new SendGroupMessageInput(
                                    sendGroupMsg.GroupId,
                                    ConvertFromMessageChain(sendGroupMsg.Message)
                                ),
                                token
                            );
                            break;
                        }
                        catch (MilkyException e) when (e.Message is "Internal server error")
                        {
                            if (triesRemain > 0)
                            {
                                _logger.LogSendingMessageFailedDueToInternalServerError(triesRemain);
                                continue;
                            }

                            throw;
                        }
                    }

                    return new SendMessageResponse(true, 0, null,
                        GenerateMessageId(MessageScene.Group, sendGroupMsg.GroupId, result.MessageSeq)
                    ) as TResp;
                }
            case SendGroupPokeRequest sendGroupPoke:
                {
                    await client.Group.SendGroupNudgeAsync(new SendGroupNudgeInput(sendGroupPoke.GroupId, sendGroupPoke.UserId), token);
                    return Response.OK as TResp;
                }
            case SendPrivateMessageRequest sendPrivateMsg:
                {
                    SendPrivateMessageOutput result;
                    var triesRemain = options.SendMessageMaxRetry + 1;
                    while (true)
                    {
                        triesRemain--;
                        try
                        {
                            result = await client.Message.SendPrivateMessageAsync(
                                new SendPrivateMessageInput(
                                    sendPrivateMsg.UserId,
                                    ConvertFromMessageChain(sendPrivateMsg.Message)
                                ),
                                token
                            );
                            break;
                        }
                        catch (MilkyException e) when (e.Message is "Internal server error")
                        {
                            if (triesRemain > 0)
                            {
                                _logger.LogSendingMessageFailedDueToInternalServerError(triesRemain);
                                continue;
                            }

                            throw;
                        }
                    }
                    return new SendMessageResponse(true, 0, null,
                        GenerateMessageId(MessageScene.Friend, sendPrivateMsg.UserId, result.MessageSeq)
                    ) as TResp;
                }
            case SetFriendAddRequestRequest setFriendAddReq:
                {
                    if (setFriendAddReq.Approve)
                    {
                        await client.Friend.AcceptFriendRequestAsync(new(setFriendAddReq.Flag, false), token);
                    }
                    else
                    {
                        await client.Friend.RejectFriendRequestAsync(new(setFriendAddReq.Flag, false, setFriendAddReq.Remark), token);
                    }
                    return Response.OK as TResp;
                }
            case SetGroupReactionRequest setGroupReaction:
                {
                    await client.Group.SendGroupMessageReactionAsync(new(
                        setGroupReaction.GroupId,
                        Convert.ToInt64(setGroupReaction.MessageId.Split('|')[2]),
                        setGroupReaction.Code,
                        setGroupReaction.IsAdd
                    ), token);
                    return Response.OK as TResp;
                }
            case UploadGroupFileRequest uploadGroupFile:
                {
                    await client.File.UploadGroupFileAsync(new(
                        uploadGroupFile.GroupId,
                        uploadGroupFile.Folder,
                        new(uploadGroupFile.File),
                        uploadGroupFile.Name
                    ), token);
                    return Response.OK as TResp;
                }
            default:
                throw new NotSupportedException($"Unsupported request type: {request.GetType().Name}");
        }
    }

    private static MessageScene GetMessageScene(IncomingMessage msg) =>
        msg switch
        {
            FriendIncomingMessage => MessageScene.Friend,
            GroupIncomingMessage => MessageScene.Group,
            TempIncomingMessage => MessageScene.Temp,
            _ => throw new ArgumentOutOfRangeException(nameof(msg))
        };

    private static MessageScene GetMessageScene(string messageScene) =>
        messageScene switch
        {
            "friend" => MessageScene.Friend,
            "group" => MessageScene.Group,
            "temp" => MessageScene.Temp,
            _ => throw new ArgumentOutOfRangeException(nameof(messageScene))
        };

    private static MessageType GetMessageType(IncomingMessage msg) =>
        msg switch
        {
            FriendIncomingMessage => MessageType.Friend,
            GroupIncomingMessage => MessageType.Group,
            TempIncomingMessage => MessageType.Temp,
            _ => throw new ArgumentOutOfRangeException(nameof(msg))
        };

    private static string GenerateMessageId(MessageScene messageScene, long peerId, long messageSeq) =>
        string.Join('|', messageScene switch
        {
            MessageScene.Friend => "friend",
            MessageScene.Group => "group",
            MessageScene.Temp => "temp",
            _ => throw new ArgumentOutOfRangeException(nameof(messageScene))
        }, peerId, messageSeq);

    private static string GenerateMessageId(IncomingMessage msg) => GenerateMessageId(GetMessageScene(msg), msg.PeerId, msg.MessageSeq);

    private MessageChain ConvertSegment(IncomingMessage msg) => [.. msg.Segments.SelectMany(seg =>
        seg switch
        {
            TextIncomingSegment text => [new TextData(text.Data.Text)],
            MentionIncomingSegment mention => [new AtData(mention.Data.UserId)],
            MentionAllIncomingSegment => [new AtData(0)],
            ReplyIncomingSegment reply => [new ReplyData(GenerateMessageId(GetMessageScene(msg), msg.PeerId, reply.Data.MessageSeq))],
            ImageIncomingSegment image => [
                new ImageData(
                        File: image.Data.TempUrl,
                        Type: image.Data.SubType switch {
                            SubType.Normal => ImageSubType.Normal,
                            SubType.Sticker => ImageSubType.Sticker,
                            _ => ImageSubType.Normal
                        },
                        Url: image.Data.TempUrl,
                        Summary: image.Data.Summary
                    )
            ],
            RecordIncomingSegment record => [new RecordData(File: record.Data.TempUrl, Url: record.Data.TempUrl)],
            VideoIncomingSegment video => [new VideoData(File: video.Data.TempUrl, Url: video.Data.TempUrl)],
            ForwardIncomingSegment forward => [new ForwardData(forward.Data.ForwardId)],
            _ => UnknownSegment(seg.GetType().Name)
        })
    ];

    private static OutgoingSegment[] ConvertFromMessageChain(MessageChain chain) => [.. chain.Select<SegmentData, OutgoingSegment>(seg => seg switch
    {
        TextData text => new TextOutgoingSegment(new(text.Text)),
        AtData at => at.Uin switch
        {
            0 => new MentionAllOutgoingSegment(new()),
            var uin => new MentionOutgoingSegment(new(uin))
        },
        ReplyData reply => new ReplyOutgoingSegment(new(Convert.ToInt64(reply.Id.Split('|')[2]))),
        ImageData image => new ImageOutgoingSegment(new(new(image.File), image.Summary, image.Type switch
        {
            ImageSubType.Normal => SubType.Normal,
            ImageSubType.Sticker => SubType.Sticker,
            _ => SubType.Normal
        })),
        RecordData record => new RecordOutgoingSegment(new(new(record.File))),
        // VideoData video => new VideoOutgoingSegment(new(new(video.File), null)),
        _ => throw new NotSupportedException($"Unsupported outgoing segment type: {seg.GetType().Name}")
    })];

    private static ForwardOutgoingSegment ConvertFromCustomNodes(List<CustomNodeData> nodes) => new(new ForwardOutgoingSegmentData([
        .. nodes.Select(node => new OutgoingForwardedMessage(node.Sender, node.Name, ConvertFromMessageChain(node.Content)))
    ]));

    private IEnumerable<SegmentData> UnknownSegment(string segmentType)
    {
        _logger.LogUnknownSegmentType(segmentType);
        return [];
    }

    private static MessageSender ConvertSender(FriendIncomingMessage friend) =>
        new(friend.Friend.UserId, friend.Friend.Nickname, ConvertSex(friend.Friend.Sex), null);


    private void RegisterEventHandlers(CancellationToken token)
    {
        var events = (MilkyEventScheduler)client.Events;

        events.MessageReceive += async (sender, e) =>
        {
            BotEvent @event;
            switch (e.Data)
            {
                case FriendIncomingMessage friend:
                    @event = new PrivateMessageEvent(
                        friend.Time.ToUnixTimeSeconds(),
                        GenerateMessageId(e.Data),
                        friend.PeerId,
                        ConvertSegment(e.Data),
                        null,
                        ConvertSender(friend)
                    );
                    break;
                case GroupIncomingMessage group:
                    @event = new GroupMessageEvent(
                        group.Time.ToUnixTimeSeconds(),
                        GenerateMessageId(e.Data),
                        group.PeerId,
                        group.SenderId,
                        null,
                        ConvertSegment(e.Data),
                        null,
                        new(
                            group.GroupMember.UserId,
                            group.GroupMember.Nickname,
                            group.GroupMember.Card,
                            ConvertSex(group.GroupMember.Sex),
                            null,
                            null,
                            group.GroupMember.Level,
                            ConvertRole(group.GroupMember.Role),
                            group.GroupMember.Title
                        )
                    );
                    break;
                default:
                    var type = e.Data.GetType().Name;
                    _logger.LogUnknownMessageScene(type);
                    return;
            }

            if (OnEventAsync is { } handler)
                await handler(@event, token);
        };
        events.MessageRecall += async (sender, e) =>
        {
            BotEvent @event;
            switch (e.Data.MessageScene)
            {
                case MessageScene.Friend:
                    @event = new FriendRecallEvent(
                       e.Time.ToUnixTimeSeconds(),
                       e.Data.SenderId,
                       GenerateMessageId(e.Data.MessageScene, e.Data.PeerId, e.Data.MessageSeq)
                   );
                    break;
                case MessageScene.Group:
                    @event = new GroupRecallEvent(
                        e.Time.ToUnixTimeSeconds(),
                        e.Data.SenderId,
                        GenerateMessageId(e.Data.MessageScene, e.Data.PeerId, e.Data.MessageSeq),
                        e.Data.PeerId,
                        e.Data.OperatorId
                    );
                    break;
                default:
                    var messageScene = e.Data.MessageScene.ToString();
                    _logger.LogUnknownMessageScene(messageScene);
                    return;
            }

            if (OnEventAsync is { } handler)
                await handler(@event, token);
        };
        events.FriendRequest += async (sender, e) =>
        {
            var @event = new Abstractions.Event.Request.FriendRequestEvent(
                e.Time.ToUnixTimeSeconds(),
                e.Data.InitiatorId,
                e.Data.Comment,
                e.Data.InitiatorUid
            );

            if (OnEventAsync is { } handler)
                await handler(@event, token);
        };
        events.GroupNudge += async (sender, e) =>
        {
            var @event = new GroupPokeEvent(
                e.Time.ToUnixTimeSeconds(),
                e.Data.GroupId,
                e.Data.SenderId,
                e.Data.ReceiverId
            );

            if (OnEventAsync is { } handler)
                await handler(@event, token);
        };
        events.GroupMemberDecrease += async (sender, e) =>
        {
            var @event = new GroupLeaveEvent(
                e.Time.ToUnixTimeSeconds(),
                e.Data.GroupId,
                e.Data.OperatorId ?? e.Data.UserId,
                e.Data.UserId
            );

            if (OnEventAsync is { } handler)
                await handler(@event, token);
        };
    }

    private async Task ReceiveLoop(ClientWebSocket ws, CancellationToken token)
    {
        await using MemoryStream ms = new();
        Memory<byte> buffer = new byte[4096];

        while (!token.IsCancellationRequested)
        {
            var result = await ws.ReceiveAsync(buffer, token);
            await ms.WriteAsync(buffer[..result.Count], token);

            if (result.EndOfMessage)
            {
                ms.Seek(0, SeekOrigin.Begin);
                if (await JsonSerializer.DeserializeAsync(ms, _milkyJsonSerializerContext.Event, token) is { } data)
                    client.Events.Received(data);
                ms.SetLength(0);
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        var baseUri = new Uri(options.Url);
        client = new MilkyClient(new HttpClient
        {
            BaseAddress = baseUri,
            DefaultRequestHeaders =
            {
                { "Authorization", $"Bearer {options.AccessToken}" }
            }
        });

        RegisterEventHandlers(token);

        using var ws = new ClientWebSocket();
        var wsUri = new UriBuilder(baseUri)
        {
            Scheme = baseUri.Scheme == Uri.UriSchemeHttp ? Uri.UriSchemeWs : Uri.UriSchemeWss,
            Path = "/event",
        }.Uri;

        while (true)
        {
            try
            {
                _logger.LogConnectingWebSocket(wsUri);
                await ws.ConnectAsync(wsUri, token);
                _logger.LogConnectedWebSocket(wsUri);

                await ReceiveLoop(ws, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (WebSocketException e)
            {
                _logger.LogWebSocketException(e);
                _logger.LogReconnect(options.ReconnectInterval);
                var interval = TimeSpan.FromSeconds(options.ReconnectInterval);
                await Task.Delay(interval, token);
            }
            catch (ObjectDisposedException)
            {
                _logger.LogReconnect(options.ReconnectInterval);
                var interval = TimeSpan.FromSeconds(options.ReconnectInterval);
                await Task.Delay(interval, token);
            }
        }
    }
}

internal static partial class MilkyClientServiceLoggerExtension
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Connecting to websocket at {Uri}")]
    public static partial void LogConnectingWebSocket(this ILogger<MilkyClientService> logger, Uri uri);

    [LoggerMessage(Level = LogLevel.Information, Message = "Connected to websocket at {Uri}")]
    public static partial void LogConnectedWebSocket(this ILogger<MilkyClientService> logger, Uri uri);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Websocket throws an exception")]
    public static partial void LogWebSocketException(this ILogger<MilkyClientService> logger, WebSocketException exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Connection closed, reconnect after {Interval} seconds")]
    public static partial void LogReconnect(this ILogger<MilkyClientService> logger, int interval);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unknown segment type: {SegmentType}")]
    public static partial void LogUnknownSegmentType(this ILogger<MilkyClientService> logger, string segmentType);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unknown message scene: {MessageScene}")]
    public static partial void LogUnknownMessageScene(this ILogger<MilkyClientService> logger, string messageScene);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sending message failed due to internal server error, remaining tries: {TriesRemain}")]
    public static partial void LogSendingMessageFailedDueToInternalServerError(this ILogger<MilkyClientService> logger, int triesRemain);
}