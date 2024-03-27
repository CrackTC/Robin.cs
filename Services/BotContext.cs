using Robin.Abstractions.Communication;

namespace Robin.Services;

// scoped, every bot has its own option
internal class BotContext : IDisposable
{
    internal long Uin { get; set; }
    internal IBotEventInvoker? EventInvoker { get; set; } = null;
    internal IOperationProvider? OperationProvider { get; set; } = null;

    public void Dispose()
    {
        EventInvoker?.Dispose();
        OperationProvider?.Dispose();
    }
}