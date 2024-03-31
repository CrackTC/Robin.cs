using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Communication;

namespace Robin.App.Services;

// singleton, create bots on startup
internal partial class BotCreationService(
    ILogger<BotCreationService> logger,
    IServiceProvider service,
    IConfiguration config) : IHostedService
{
    private readonly List<(IServiceScope, BotFunctionService)> _scopedServices = [];
    public async Task StartAsync(CancellationToken token)
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

            option.EventInvoker = await eventInvokerFactory.GetBotEventInvokerAsync(section.GetRequiredSection("EventInvokerConfig"), token);
            option.OperationProvider = await operationProviderFactory.GetOperationProviderAsync(section.GetRequiredSection("OperationProviderConfig"), token);
            option.FunctionConfigurations = section.GetSection("Configurations")
                .GetChildren()
                .ToDictionary(child => child["Name"]!);

            var functionService = scope.ServiceProvider.GetRequiredService<BotFunctionService>();
            await functionService.StartAsync(token);
            _scopedServices.Add((scope, functionService));

            LogBotStarted(logger, option.Uin);
        }
    }

    public async Task StopAsync(CancellationToken token)
    {
        foreach (var (scope, functionService) in _scopedServices)
        {
            await functionService.StopAsync(token);
            LogBotStopped(logger, scope.ServiceProvider.GetRequiredService<BotContext>().Uin);
            scope.Dispose();
        }
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Bot {Uin} started")]
    private static partial void LogBotStarted(ILogger logger, long uin);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Bot {Uin} stopped")]
    private static partial void LogBotStopped(ILogger logger, long uin);

    #endregion
}
