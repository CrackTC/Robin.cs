using Microsoft.Extensions.Configuration;
using Robin.Abstractions.Communication;

namespace Robin.App.Services;

// scoped, every bot has its own option
internal class BotContext : IDisposable
{
    internal long Uin { get; set; }
    internal IBotEventInvoker? EventInvoker { get; set; }
    internal IOperationProvider? OperationProvider { get; set; }
    internal Dictionary<string, IConfigurationSection>? FunctionConfigurations { get; set; }

    public void Dispose()
    {
        EventInvoker?.Dispose();
        OperationProvider?.Dispose();

        GC.SuppressFinalize(this);
    }
}
