using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Robin.Abstractions;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Context;
using Robin.App;
using Robin.App.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("config.json");
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "MM-dd HH:mm:ss ";
    options.ColorBehavior = LoggerColorBehavior.Enabled;
});

var implementations = LoadAssemblies("Implementations");
var middlewares = LoadAssemblies("Middlewares");
var extensions = LoadAssemblies("Extensions");
ConfigureBackend(implementations);

builder
    .Services.AddHostedService<BotCreationService>()
    .AddSingleton<IEnumerable<Assembly>>([.. middlewares, .. extensions])
    .AddScoped<BotFunctionService>()
    .AddScoped<BotContext>()
    .AddScoped<List<BotFunction>>();

await builder.Build().RunAsync();
return;

void ConfigureBackend(IEnumerable<Assembly> implementations)
{
    var backends = implementations
        .SelectMany(assembly => assembly.GetExportedTypes())
        .Select(type => (Type: type, Attributes: type.GetCustomAttributes<BackendAttribute>(false)))
        .Where(pair => pair.Attributes.Any() && pair.Type.IsAssignableTo(typeof(IBackendFactory)))
        .SelectMany(pair => pair.Attributes.Select(attribute => (attribute.Name, pair.Type)));

    foreach (var (name, type) in backends)
    {
        Console.WriteLine($"Found backend: {name} -> {type}");
        builder.Services.AddKeyedScoped(typeof(IBackendFactory), name, type);
    }
}

IEnumerable<Assembly> LoadAssemblies(string dir)
{
    var path = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory) ?? string.Empty, dir);
    return
    [
        .. Directory
            .GetDirectories(path)
            .Select(subDir =>
                new BotExtensionLoadContext(
                    Path.Combine(subDir, $"{Path.GetFileName(subDir)}.dll")
                ).LoadFromAssemblyName(new AssemblyName(Path.GetFileName(subDir)))
            ),
    ];
}
