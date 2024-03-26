using Robin.Abstractions.Operation;

namespace Robin.Abstractions.Communication;

public interface IOperationProvider
{
    Task<Response> SendRequestAsync(Request request, CancellationToken cancellationToken = default);
}