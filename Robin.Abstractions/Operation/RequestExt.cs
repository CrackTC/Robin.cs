using Robin.Abstractions.Communication;

namespace Robin.Abstractions.Operation;

public static class RequestExt
{
    public static Task<Response?> SendAsync(this Request request, IOperationProvider operation, CancellationToken token = default)
        => operation.SendRequestAsync(request, token);
}