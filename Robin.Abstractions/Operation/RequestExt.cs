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
            var resp = await context.BotContext.OperationProvider.SendRequestAsync(request, token);
            LogSent(context.Logger, request, resp);
            return resp;
        }
        catch (Exception e)
        {
            LogSendException(context.Logger, request, e);
            return null;
        }
    }

    #region Log

    [LoggerMessage(Level = LogLevel.Information, Message = "Sent request: {Request}, response: {Response}")]
    private static partial void LogSent(ILogger logger, Request request, Response response);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Exception occured when sending request: {Request}")]
    private static partial void LogSendException(ILogger logger, Request request, Exception e);

    #endregion
}
