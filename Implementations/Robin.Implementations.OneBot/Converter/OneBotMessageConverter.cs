using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Implementations.OneBot.Entity.Message;
using Robin.Implementations.OneBot.Entity.Message.Data;

namespace Robin.Implementations.OneBot.Converter;

internal partial class OneBotMessageConverter(ILogger<OneBotMessageConverter> logger)
{
    private static readonly Dictionary<string, Type> _typeNameToDataType = [];
    private static readonly Dictionary<Type, Type> _segmentTypeToDataType = [];

    static OneBotMessageConverter()
    {
        var dataTypes = typeof(OneBotMessageConverter).Assembly
            .GetTypes()
            .Select(type => (Type: type, Attribute: type.GetCustomAttribute<OneBotSegmentDataAttribute>()))
            .Where(pair => pair.Attribute is not null && pair.Type.IsAssignableTo(typeof(IOneBotSegmentData)));

        foreach (var (type, attribute) in dataTypes)
        {
            _typeNameToDataType[attribute!.TypeName] = type;
            foreach (var segmentType in attribute.Types)
            {
                _segmentTypeToDataType[segmentType] = type;
            }
        }
    }

    public MessageChain? ParseMessageChain(JsonNode? messageNode)
    {
        if (messageNode is null) return null;
        switch (messageNode.GetValueKind())
        {
            case JsonValueKind.Array:
                return ParseFromArray(messageNode.AsArray());
            case JsonValueKind.String:
                return ParseFromString(messageNode.GetValue<string>());
            case JsonValueKind.Undefined:
            case JsonValueKind.Object:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
            default:
                LogInvalidMessageType(logger, messageNode.ToJsonString());
                return null;
        }
    }

    private OneBotSegment? FromSegmentData(SegmentData segmentData)
    {
        var dataType = _segmentTypeToDataType[segmentData.GetType()];
        if (Activator.CreateInstance(dataType) is IOneBotSegmentData data)
        {
            return data.FromSegmentData(segmentData, this);
        }

        LogCreateInstanceFailed(logger, dataType.Name);
        return null;
    }

    public JsonArray? SerializeToArray(MessageChain chain)
    {
        var list = new List<OneBotSegment>();
        foreach (var segmentData in chain)
        {
            if (FromSegmentData(segmentData) is not { } data) return null;
            list.Add(data);
        }

        return new JsonArray(list.Select(segment => JsonSerializer.SerializeToNode(segment)).ToArray());
    }

    private MessageChain? ParseFromArray(JsonArray segmentArray)
    {
        var chain = new MessageChain();

        foreach (var segmentNode in segmentArray)
        {
            if (segmentNode.Deserialize<OneBotSegment>() is not { } segment)
            {
                LogInvalidSegmentType(logger, segmentNode?.ToJsonString() ?? string.Empty);
                return null;
            }

            if (!_typeNameToDataType.TryGetValue(segment.Type, out var dataType))
            {
                LogDataTypeNotFound(logger, segment.Type);
                return null;
            }

            if (segment.Data.Deserialize(dataType) is not IOneBotSegmentData data)
            {
                LogInvalidSegmentData(logger, segment.Data?.ToJsonString() ?? string.Empty);
                return null;
            }

            chain.Add(data.ToSegmentData(this));
        }

        return chain;
    }

    [GeneratedRegex(@"\[CQ:([^,\]]+)(?:,([^,\]]+))*\]")]
    private static partial Regex CqCodeRegex { get; }

    private static string UnescapeCq(string str)
        => str.Replace("&#91;", "[")
            .Replace("&#93;", "]")
            .Replace("&#44;", ",")
            .Replace("&amp;", "&");

    private static string UnescapeText(string str)
        => str.Replace("&#91;", "[")
            .Replace("&#93;", "]")
            .Replace("&amp;", "&");

    private MessageChain? ParseFromString(string segmentString)
    {
        var chain = new MessageChain();

        var matches = CqCodeRegex.Matches(segmentString);
        var textStart = 0;
        foreach (Match match in matches)
        {
            if (match.Index > textStart) chain.Add(new TextData(UnescapeText(segmentString[textStart..match.Index])));
            textStart = match.Index + match.Length;

            var type = match.Groups[1].Value;
            if (!_typeNameToDataType.TryGetValue(type, out var dataType))
            {
                LogDataTypeNotFound(logger, type);
                return null;
            }

            var cqData = new Dictionary<string, string>();
            foreach (Capture capture in match.Groups[2].Captures)
            {
                var pair = capture.Value.Split('=', 2);
                if (pair.Length != 2)
                {
                    LogInvalidCqCode(logger, segmentString);
                    return null;
                }

                cqData[pair[0]] = UnescapeCq(pair[1]);
            }

            var node = JsonSerializer.SerializeToNode(cqData)!;
            if (node.Deserialize(dataType) is not IOneBotSegmentData data)
            {
                LogInvalidSegmentData(logger, node.ToString());
                return null;
            }

            chain.Add(data.ToSegmentData(this));
        }

        if (textStart < segmentString.Length)
            chain.Add(new TextData(UnescapeText(segmentString[textStart..])));

        return chain;
    }

    #region Log

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid message type: {Json}")]
    private static partial void LogInvalidMessageType(ILogger logger, string json);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid segment type: {Json}")]
    private static partial void LogInvalidSegmentType(ILogger logger, string json);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Data type {Type} not found")]
    private static partial void LogDataTypeNotFound(ILogger logger, string type);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid segment data: {Json}")]
    private static partial void LogInvalidSegmentData(ILogger logger, string json);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid CQ code: {Code}")]
    private static partial void LogInvalidCqCode(ILogger logger, string code);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to create instance of type {Type}")]
    private static partial void LogCreateInstanceFailed(ILogger logger, string type);

    #endregion
}
