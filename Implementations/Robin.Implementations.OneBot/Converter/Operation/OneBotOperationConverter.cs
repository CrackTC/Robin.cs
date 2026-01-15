using System.Collections.Frozen;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Operation;

namespace Robin.Implementations.OneBot.Converter.Operation;

internal partial class OneBotOperationConverterProvider
{
    private readonly ILogger<OneBotOperationConverterProvider> _logger;
    private readonly FrozenDictionary<Type, (IOneBotRequestConverter, IOneBotResponseConverter)> _reqTypeToConverters;

    public OneBotOperationConverterProvider(string? variant, ILogger<OneBotOperationConverterProvider> logger)
    {
        _logger = logger;

        Dictionary<Type, Type> respTypeToConverterType = [];
        foreach (var type in typeof(OneBotOperationConverterProvider).Assembly.GetTypes())
        {
            if (!type.IsAssignableTo(typeof(IOneBotResponseConverter))) continue;
            if (type.GetInterfaces().FirstOrDefault(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IOneBotResponseConverter<>))?.GetGenericArguments()[0] is not { } respType) continue;

            var variants = type.GetCustomAttribute<OneBotVariantAttribute>()?.Variants ?? [];
            if (variants is [] && !respTypeToConverterType.ContainsKey(respType)) respTypeToConverterType[respType] = type;
            if (variants.Any(v => v == variant)) respTypeToConverterType[respType] = type;
        }

        Dictionary<Type, (Type, Type)> reqTypeToConverterTypes = [];
        foreach (var type in typeof(OneBotOperationConverterProvider).Assembly.GetTypes())
        {
            if (!type.IsAssignableTo(typeof(IOneBotRequestConverter))) continue;
            if (type.GetInterfaces().FirstOrDefault(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IOneBotRequestConverter<>))?.GetGenericArguments()[0] is not { } reqType) continue;

            if (reqType.GetInterfaces().FirstOrDefault(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(RequestFor<>))?.GetGenericArguments()[0] is not { } respType) continue;

            if (respTypeToConverterType.GetValueOrDefault(respType) is not { } respConverterType) continue;

            var variants = type.GetCustomAttribute<OneBotVariantAttribute>()?.Variants ?? [];
            if (variants is [] && !reqTypeToConverterTypes.ContainsKey(reqType)) reqTypeToConverterTypes[reqType] = (type, respConverterType);
            if (variants.Any(v => v == variant)) reqTypeToConverterTypes[reqType] = (type, respConverterType);
        }

        _reqTypeToConverters = reqTypeToConverterTypes.ToFrozenDictionary(
            kvp => kvp.Key,
            kvp => (
                (IOneBotRequestConverter)Activator.CreateInstance(kvp.Value.Item1)!,
                (IOneBotResponseConverter)Activator.CreateInstance(kvp.Value.Item2)!
            )
        );
    }

    public IOneBotRequestConverter<TReq> GetRequestConverter<TReq>(TReq request) where TReq : Request
    {
        if (_reqTypeToConverters.GetValueOrDefault(request.GetType()) is (IOneBotRequestConverter<TReq> reqConverter, _))
            return reqConverter;

        LogOneBotConverterNotFound(_logger, request);
        throw new();
    }

    public IOneBotResponseConverter<TResp> GetResponseConverter<TResp>(RequestFor<TResp> request) where TResp : Response
    {
        if (_reqTypeToConverters.GetValueOrDefault(request.GetType()) is (_, IOneBotResponseConverter<TResp> respConverter))
            return respConverter;

        LogOneBotConverterNotFound(_logger, request);
        throw new();
    }

    #region Log

    [LoggerMessage(Level = LogLevel.Warning, Message = "OneBotConverter for request {Request} not found")]
    private static partial void LogOneBotConverterNotFound(ILogger logger, Request request);

    #endregion
}
