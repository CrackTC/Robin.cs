using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Operation;
using Robin.Implementations.OneBot.Entities.Operations;

namespace Robin.Implementations.OneBot.Converters;

internal partial class OneBotOperationConverter(ILogger<OneBotOperationConverter> logger)
{
    private static readonly Dictionary<Type, (string, IOneBotRequest)> _requestTypeToOneBotRequest = [];
    private static readonly Dictionary<Type, Type> _requestTypeToResponseType = [];

    static OneBotOperationConverter()
    {
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Select(type => (Type: type, Attribute: type.GetCustomAttribute<OneBotRequestAttribute>()))
            .Where(pair => pair.Attribute is not null && pair.Type.IsAssignableTo(typeof(IOneBotRequest)));

        foreach (var (type, attribute) in types)
        {
            if (Activator.CreateInstance(type) is not IOneBotRequest r) continue;
            _requestTypeToOneBotRequest[attribute!.Type] = (attribute.Endpoint, r);
        }

        var types1 = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Select(type => (Type: type, Attribute: type.GetCustomAttribute<OneBotResponseDataAttribute>()))
            .Where(pair => pair.Attribute is not null && pair.Type.IsAssignableTo(typeof(IOneBotResponseData)));

        foreach (var (type, attribute) in types1)
            _requestTypeToResponseType[attribute!.RequestType] = type;
    }

    public (string, JsonNode?, Type)? SerializeToJson(Request request, OneBotMessageConverter converter)
    {
        if (_requestTypeToOneBotRequest.TryGetValue(request.GetType(), out var pair))
            return (pair.Item1, pair.Item2.GetJson(request, converter), pair.Item2.GetType());

        LogOneBotRequestNotFound(logger, request);
        return null;
    }

    public Response? ParseResponse(Type requestType, OneBotResponse response, OneBotMessageConverter converter)
    {
        if (response.Status == "failed") return new Response(false, response.ReturnCode, null);

        // common response with null data
        if (!_requestTypeToResponseType.TryGetValue(requestType, out var dataType))
        {
            if (response.Data is not null) LogIgnoringData(logger, response.Data.ToJsonString());
            return new Response(response.Status is not "failed", response.ReturnCode, null);
        }

        // response with data
        if (response.Data.Deserialize(dataType) is IOneBotResponseData data)
            return data.ToResponse(response, converter);

        LogDeserializeDataFailed(logger, response.Data!.ToJsonString());
        return null;
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "OneBotRequest for request {Request} not found")]
    private static partial void LogOneBotRequestNotFound(ILogger logger, Request request);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Failed to deserialize response data: {Data}")]
    private static partial void LogDeserializeDataFailed(ILogger logger, string data);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Ignoring response data: {Data}")]
    private static partial void LogIgnoringData(ILogger logger, string data);

    #endregion
}