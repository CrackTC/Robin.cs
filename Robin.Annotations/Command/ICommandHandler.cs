using Robin.Abstractions.Event.Message;

namespace Robin.Annotations.Command;

public interface ICommandHandler
{
    Task OnCommandAsync(long selfId, MessageEvent @event, CancellationToken token);
}