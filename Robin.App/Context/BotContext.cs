using System.Collections.Frozen;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Context;

namespace Robin.App.Context;

// scoped, every bot has its own option
internal class BotContext(
    IServiceProvider serviceProvider,
    List<BotFunction> functions
) : IDisposable
{
    public long Uin { get; set; }
    public IBotEventInvoker? EventInvoker { get; set; }
    public IOperationProvider? OperationProvider { get; set; }
    public FrozenDictionary<string, IConfigurationSection>? FunctionConfigurations { get; set; }

    public FunctionContext CreateFunctionContext(string functionName, Type functionType)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(functionType);
        FunctionConfigurations!.TryGetValue(functionName, out var configuration);

        return new FunctionContext(logger, Uin, OperationProvider!, configuration!, functions);
    }

    public void Dispose()
    {
        EventInvoker?.Dispose();
        OperationProvider?.Dispose();

        GC.SuppressFinalize(this);
    }
}
