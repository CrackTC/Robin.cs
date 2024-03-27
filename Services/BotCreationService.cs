using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Robin.Abstractions.Communication;

namespace Robin.Services;

// singleton, create bots on startup
internal class BotCreationService(IServiceProvider service, IConfiguration config) : IHostedService
{
    private readonly List<(IServiceScope, BotFunctionService)> _scopedServices = [];
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var botSections = config.GetSection("Bots").GetChildren();
        foreach (var section in botSections)
        {
            var scope = service.CreateScope();
            var option = scope.ServiceProvider.GetRequiredService<BotContext>();
            option.Uin = long.Parse(section["Uin"] ?? throw new InvalidOperationException("Uin is not set"));

            var eventInvokerName = section["EventInvokerName"];
            var eventInvokerFactory = service.GetRequiredKeyedService<IBackendFactory>(eventInvokerName);

            var operationProviderName = section["OperationProviderName"];
            var operationProviderFactory = service.GetRequiredKeyedService<IBackendFactory>(operationProviderName);

            option.EventInvoker = eventInvokerFactory.GetBotEventInvoker(section.GetRequiredSection("EventInvokerConfig"));
            option.OperationProvider = operationProviderFactory.GetOperationProvider(section.GetRequiredSection("OperationProviderConfig"));

            var functionService = scope.ServiceProvider.GetRequiredService<BotFunctionService>();
            await functionService.StartAsync(cancellationToken);
            _scopedServices.Add((scope, functionService));
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var (scope, functionService) in _scopedServices)
        {
            await functionService.StopAsync(cancellationToken);
            scope.Dispose();
        }
    }
}