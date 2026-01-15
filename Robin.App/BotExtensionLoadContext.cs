using System.Reflection;
using System.Runtime.Loader;

namespace Robin.App;

internal class BotExtensionLoadContext(string extensionPath) : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _extDirResolver = new(extensionPath);
    private static readonly WeakReference<Dictionary<string, Assembly>> _baseDirAssemblies = new(
        AppDomain
            .CurrentDomain.GetAssemblies()
            .ToDictionary(assembly => assembly.GetName().Name!, assembly => assembly)
    );
    private static readonly Dictionary<string, Assembly> _loadedExtAssemblies = [];
    private static readonly Dictionary<string, nint?> _loadedExtUnmanagedDlls = [];

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (_loadedExtAssemblies.GetValueOrDefault(assemblyName.Name!) is { } assembly)
        {
            // Previously loaded assembly from extension directory
            return assembly;
        }

        if (!_baseDirAssemblies.TryGetTarget(out var baseDirAssemblies))
        {
            baseDirAssemblies = AppDomain
                .CurrentDomain.GetAssemblies()
                .ToDictionary(assembly => assembly.GetName().Name!, assembly => assembly);
            _baseDirAssemblies.SetTarget(baseDirAssemblies);
        }

        if (baseDirAssemblies.GetValueOrDefault(assemblyName.Name!) is { } baseAssembly)
        {
            // Assembly already loaded from base directory
            return baseAssembly;
        }

        if (_extDirResolver.ResolveAssemblyToPath(assemblyName) is { } extPath)
        {
            // Load assembly from extension directory
            var loadedAssembly = LoadFromAssemblyPath(extPath);
            _loadedExtAssemblies[assemblyName.Name!] = loadedAssembly;
            return loadedAssembly;
        }

        // Not found, likely a framework assembly, let framework handle it
        return null;
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        if (_loadedExtUnmanagedDlls.GetValueOrDefault(unmanagedDllName) is { } handle)
        {
            // Previously loaded unmanaged DLL from extension directory
            return handle;
        }
        if (_extDirResolver.ResolveUnmanagedDllToPath(unmanagedDllName) is { } extPath)
        {
            // Load unmanaged DLL from extension directory
            var loadedUnmanagedDll = LoadUnmanagedDllFromPath(extPath);
            _loadedExtUnmanagedDlls[unmanagedDllName] = loadedUnmanagedDll;
            return loadedUnmanagedDll;
        }
        // I don't know, let framework handle it
        return nint.Zero;
    }
}
