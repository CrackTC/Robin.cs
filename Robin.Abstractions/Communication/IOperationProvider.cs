using Robin.Abstractions.Operation;

namespace Robin.Abstractions.Communication;

public interface IOperationProvider : IDisposable
{
    Task<TResp?> SendRequestAsync<TResp>(RequestFor<TResp> request, CancellationToken token) where TResp : Response;
}
