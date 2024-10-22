using System.Reflection;
using System.Runtime.Loader;

namespace Robin.App;

internal class BotExtensionLoadContext : AssemblyLoadContext
{
    private AssemblyDependencyResolver _resolver;

    public BotExtensionLoadContext(string extensionPath)
    {
        _resolver = new AssemblyDependencyResolver(extensionPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName) =>
        _resolver.ResolveAssemblyToPath(assemblyName) switch
        {
            { } path => LoadFromAssemblyPath(path),
            _ => null
        };

    protected override nint LoadUnmanagedDll(string unmanagedDllName) =>
        _resolver.ResolveUnmanagedDllToPath(unmanagedDllName) switch
        {
            { } path => LoadUnmanagedDllFromPath(path),
            _ => nint.Zero
        };
}