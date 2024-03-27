using Robin.Abstractions.Operation;

namespace Robin.Abstractions.Communication;

public interface IOperationProvider : IDisposable
{
    Task<Response> SendRequestAsync(Request request, CancellationToken cancellationToken = default);
}