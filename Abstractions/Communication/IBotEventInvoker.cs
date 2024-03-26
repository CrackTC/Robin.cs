using Robin.Abstractions.Event;

namespace Robin.Abstractions.Communication;

public interface IBotEventInvoker
{
    event Action<BotEvent> OnEvent;
}