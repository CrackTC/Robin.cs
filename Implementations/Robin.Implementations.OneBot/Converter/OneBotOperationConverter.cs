using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Operation;
using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter;

internal partial class OneBotOperationConverter(ILogger<OneBotOperationConverter> logger)
{
    private static readonly Dictionary<Type, (string, IOneBotRequest)> _requestTypeToOneBotRequest = [];
    private static readonly Dictionary<Type, Type> _requestTypeToResponseType = [];

    static OneBotOperationConverter()
    {
        var types = typeof(OneBotOperationConverter).Assembly
            .GetTypes()
            .Select(type => (Type: type, Attribute: type.GetCustomAttribute<OneBotRequestAttribute>()))
            .Where(pair => pair.Attribute is not null && pair.Type.IsAssignableTo(typeof(IOneBotRequest)));

        foreach (var (type, attribute) in types)
        {
            if (Activator.CreateInstance(type) is not IOneBotRequest r) continue;
            _requestTypeToOneBotRequest[attribute!.Type] = (attribute.Endpoint, r);
        }

        var types1 = typeof(OneBotOperationConverter).Assembly
            .GetTypes()
            .Select(type => (Type: type, Attributes: type.GetCustomAttributes<OneBotResponseDataAttribute>()))
            .Where(pair => pair.Attributes is not null && pair.Type.IsAssignableTo(typeof(IOneBotResponseData)));

        foreach (var (type, attributes) in types1)
            foreach (var attribute in attributes)
                _requestTypeToResponseType[attribute!.RequestType] = type;
    }

    public (string, JsonNode?, Type)? SerializeToJson<TResp>(RequestFor<TResp> request, OneBotMessageConverter converter) where TResp : Response
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "OneBotRequest for request {Request} not found")]
    private static partial void LogOneBotRequestNotFound(ILogger logger, Request request);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to deserialize response data: {Data}")]
    private static partial void LogDeserializeDataFailed(ILogger logger, string data);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Ignoring response data: {Data}")]
    private static partial void LogIgnoringData(ILogger logger, string data);

    #endregion
}
