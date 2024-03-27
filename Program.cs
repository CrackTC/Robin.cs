using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Robin.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("config.json");

builder.Services.AddHostedService<BotCreationService>()
    .AddScoped<BotFunctionService>()
    .AddScoped<BotContext>();

builder.Logging.AddConsole();

await builder.Build().RunAsync();