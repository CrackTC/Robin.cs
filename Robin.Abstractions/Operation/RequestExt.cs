using Microsoft.Extensions.Logging;
using Robin.Abstractions.Communication;

namespace Robin.Abstractions.Operation;

public static partial class RequestExt
{
    public static Task<Response?> SendAsync(
        this Request request,
        IOperationProvider operation,
        ILogger logger,
        CancellationToken token
    ) => SendAsync<Response>(request, operation, logger, token);

    public async static Task<TResponse?> SendAsync<TResponse>(
        this Request request,
        IOperationProvider operation,
        ILogger logger,
        CancellationToken token
    ) where TResponse : Response
    {
        try
        {
            if (await operation.SendRequestAsync(request, token)
                is not { Success: true } response)
            {
                LogSendFailed(logger, request);
                return null;
            }

            if (response is not TResponse tResp)
            {
                LogMalformedResponse(logger, request, response);
                return null;
            }

            LogSent(logger, request, tResp);
            return tResp;
        }
        catch (Exception e)
        {
            LogSendFailed(logger, request, e);
            return null;
        }
    }

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Sent request: {Request}, response: {Response}")]
    private static partial void LogSent(ILogger logger, Request request, Response response);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Failed to send request: {Request}")]
    private static partial void LogSendFailed(ILogger logger, Request request, Exception? e = default);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Malformed response for request: {Request}, response: {Response}")]
    private static partial void LogMalformedResponse(ILogger logger, Request request, Response response);
}
