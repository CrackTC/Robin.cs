using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Communication;
using Robin.App.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("config.json");

builder.Logging.AddConsole();

LoadAssemblies("Implementations");
LoadAssemblies("Extensions");
ConfigureBackend();

builder.Services.AddHostedService<BotCreationService>()
    .AddScoped<BotFunctionService>()
    .AddScoped<BotContext>();

await builder.Build().RunAsync();
return;

void ConfigureBackend()
{
    var backends = AppDomain.CurrentDomain.GetAssemblies()
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

void LoadAssemblies(string dir)
{
    var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, dir);
    foreach (var dll in Directory.GetFiles(path, "*.dll"))
    {
        Assembly.LoadFile(Path.GetFullPath(dll));
    }
}
