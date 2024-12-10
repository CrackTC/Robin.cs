using System.Reflection;
using System.Runtime.Loader;

namespace Robin.App;

internal class BotExtensionLoadContext(string extensionPath) : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver = new(extensionPath);

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
