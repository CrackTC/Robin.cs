using Robin.Abstractions.Communication;

namespace Robin.Abstractions.Operation;

public static class RequestExt
{
    public async static Task<Response?> SendAsync(this Request? request, IOperationProvider operation, CancellationToken token = default)
        => request is not null ? await operation.SendRequestAsync(request, token) : new Response(false, -1, null);
}