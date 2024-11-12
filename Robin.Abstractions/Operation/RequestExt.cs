using Microsoft.Extensions.Logging;
using Robin.Abstractions.Context;

namespace Robin.Abstractions.Operation;

public static partial class RequestExt
{
    public static Task<Response?> SendAsync(
        this RequestFor<Response> request,
        FunctionContext context,
        CancellationToken token
    ) => SendAsync<Response>(request, context, token);

    public async static Task<TResp?> SendAsync<TResp>(
        this RequestFor<TResp> request,
        FunctionContext context,
        CancellationToken token
    ) where TResp : Response
    {
        try
        {
            if (await context.BotContext.OperationProvider.SendRequestAsync(request, token)
                is not { Success: true } response)
            {
                LogSendFailed(context.Logger, request);
                return null;
            }

            LogSent(context.Logger, request, response);
            return response;
        }
        catch (Exception e)
        {
            LogSendFailed(context.Logger, request, e);
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Sent request: {Request}, response: {Response}")]
    private static partial void LogSent(ILogger logger, Request request, Response response);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to send request: {Request}")]
    private static partial void LogSendFailed(ILogger logger, Request request, Exception? e = default);
}
