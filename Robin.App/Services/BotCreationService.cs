using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Context;

namespace Robin.App.Services;

// singleton, create bots on startup
internal partial class BotCreationService(
    ILogger<BotCreationService> logger,
    IServiceProvider service,
    IConfiguration config
) : IHostedService
{
    private readonly ConcurrentBag<(IServiceScope, BotFunctionService)> _scopedServices = [];

    private async Task StartBot(IConfiguration botConfig, CancellationToken token)
    {
        var scope = service.CreateScope();

        var eventInvokerName = botConfig["EventInvokerName"];
        var eventInvokerFactory = service.GetRequiredKeyedService<IBackendFactory>(
            eventInvokerName
        );

        var operationProviderName = botConfig["OperationProviderName"];
        var operationProviderFactory = service.GetRequiredKeyedService<IBackendFactory>(
            operationProviderName
        );

        var context = scope.ServiceProvider.GetRequiredService<BotContext>();
        context.Uin = long.Parse(
            botConfig["Uin"] ?? throw new InvalidOperationException("Uin is not set")
        );
        context.EventInvoker = await eventInvokerFactory.GetBotEventInvokerAsync(
            botConfig.GetRequiredSection("EventInvokerConfig"),
            token
        );
        context.OperationProvider = await operationProviderFactory.GetOperationProviderAsync(
            botConfig.GetRequiredSection("OperationProviderConfig"),
            token
        );
        context.FunctionConfigurations = botConfig.GetSection("Configurations");
        context.FilterConfigurations = botConfig.GetSection("Filters");

        var functionService = scope.ServiceProvider.GetRequiredService<BotFunctionService>();
        await functionService.StartAsync(token);
        _scopedServices.Add((scope, functionService));

        LogBotStarted(logger, context.Uin);
    }

    public Task StartAsync(CancellationToken token) =>
        Task.WhenAll(config.GetSection("Bots").GetChildren().Select(bot => StartBot(bot, token)));

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

    [LoggerMessage(Level = LogLevel.Information, Message = "Bot {Uin} started")]
    private static partial void LogBotStarted(ILogger logger, long uin);

    [LoggerMessage(Level = LogLevel.Information, Message = "Bot {Uin} stopped")]
    private static partial void LogBotStopped(ILogger logger, long uin);

    #endregion
}
