using System.Collections.Frozen;
using Microsoft.Extensions.Configuration;
using Robin.Abstractions.Communication;

namespace Robin.App.Context;

// scoped, every bot has its own option
internal class BotContext : IDisposable
{
    public long Uin { get; set; }
    public IBotEventInvoker? EventInvoker { get; set; }
    public IOperationProvider? OperationProvider { get; set; }
    public FrozenDictionary<string, IConfigurationSection>? FunctionConfigurations { get; set; }

    public void Dispose()
    {
        EventInvoker?.Dispose();
        OperationProvider?.Dispose();

        GC.SuppressFinalize(this);
    }
}
