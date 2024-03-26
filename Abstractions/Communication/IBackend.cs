using Robin.Abstractions.Event;
using Robin.Abstractions.Operation;

namespace Robin.Abstractions.Communication;

public interface IBackend
{
    Task<Response> SendRequestAsync(Request request, CancellationToken cancellationToken = default);

    event Action<BotEvent> OnEvent;
}