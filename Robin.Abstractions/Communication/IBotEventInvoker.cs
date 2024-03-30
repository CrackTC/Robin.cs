using Robin.Abstractions.Event;

namespace Robin.Abstractions.Communication;

public interface IBotEventInvoker : IDisposable
{
    event Action<BotEvent>? OnEvent;
}