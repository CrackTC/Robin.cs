using Robin.Abstractions.Event;

namespace Robin.Abstractions.Communication;

public interface IBotEventInvoker : IDisposable
{
    event Func<BotEvent, CancellationToken, Task>? OnEventAsync;
}