using System.Collections.Frozen;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Operation;

namespace Robin.Implementations.OneBot.Converter.Operation;

internal partial class OneBotOperationConverterProvider
{
    private readonly ILogger<OneBotOperationConverterProvider> _logger;
    private readonly FrozenDictionary<
        Type,
        (IOneBotRequestConverter, IOneBotResponseConverter)
    > _reqTypeToConverters;

    public OneBotOperationConverterProvider(
        string? variant,
        ILogger<OneBotOperationConverterProvider> logger
    )
    {
        _logger = logger;

        var types = typeof(OneBotOperationConverterProvider).Assembly.GetTypes();

        Dictionary<Type, Type> respTypeToConverterType = [];
        foreach (var type in types)
        {
            if (!type.IsAssignableTo(typeof(IOneBotResponseConverter)))
                continue;
            if (
                type.GetInterfaces()
                    .FirstOrDefault(i =>
                        i.IsGenericType
                        && i.GetGenericTypeDefinition() == typeof(IOneBotResponseConverter<>)
                    )
                    ?.GetGenericArguments()[0]
                is not { } respType
            )
                continue;

            var variants = type.GetCustomAttribute<OneBotVariantAttribute>()?.Variants ?? [];
            if (variants is [] && !respTypeToConverterType.ContainsKey(respType))
                respTypeToConverterType[respType] = type;
            if (variants.Any(v => v == variant))
                respTypeToConverterType[respType] = type;
        }

        Dictionary<Type, (Type, Type)> reqTypeToConverterTypes = [];
        foreach (var type in types)
        {
            if (!type.IsAssignableTo(typeof(IOneBotRequestConverter)))
                continue;

            Type? reqType = null;
            for (var t = type; t != null; t = t.BaseType)
            {
                if (!t.IsGenericType)
                    continue;
                if (t.GetGenericTypeDefinition() == typeof(OneBotRequestConverter<>))
                {
                    reqType = t.GetGenericArguments()[0];
                    break;
                }
            }

            if (reqType is null)
                continue;

            Type? respType = null;
            for (var t = reqType; t != null; t = t.BaseType)
            {
                if (!t.IsGenericType)
                    continue;
                if (t.GetGenericTypeDefinition() == typeof(RequestFor<>))
                {
                    respType = t.GetGenericArguments()[0];
                    break;
                }
            }

            if (respType is null)
                continue;

            if (respTypeToConverterType.GetValueOrDefault(respType) is not { } respConverterType)
                continue;

            var variants = type.GetCustomAttribute<OneBotVariantAttribute>()?.Variants ?? [];
            if (variants is [] && !reqTypeToConverterTypes.ContainsKey(reqType))
                reqTypeToConverterTypes[reqType] = (type, respConverterType);
            if (variants.Any(v => v == variant))
                reqTypeToConverterTypes[reqType] = (type, respConverterType);
        }

        _reqTypeToConverters = reqTypeToConverterTypes.ToFrozenDictionary(
            kvp => kvp.Key,
            kvp =>
                (
                    (IOneBotRequestConverter)Activator.CreateInstance(kvp.Value.Item1)!,
                    (IOneBotResponseConverter)Activator.CreateInstance(kvp.Value.Item2)!
                )
        );
    }

    public IOneBotRequestConverter GetRequestConverter(Request request)
    {
        if (
            _reqTypeToConverters.GetValueOrDefault(request.GetType()) is
            (IOneBotRequestConverter reqConverter, _)
        )
            return reqConverter;

        LogOneBotConverterNotFound(_logger, request);
        throw new();
    }

    public IOneBotResponseConverter<TResp> GetResponseConverter<TResp>(RequestFor<TResp> request)
        where TResp : Response
    {
        if (
            _reqTypeToConverters.GetValueOrDefault(request.GetType()) is
            (_, IOneBotResponseConverter<TResp> respConverter)
        )
            return respConverter;

        LogOneBotConverterNotFound(_logger, request);
        throw new();
    }

    #region Log

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "OneBotConverter for request {Request} not found"
    )]
    private static partial void LogOneBotConverterNotFound(ILogger logger, Request request);

    #endregion
}
