using System.Reflection;
using System.Runtime.Loader;

namespace MicroM.Web.Services;

public sealed class EntityAssemblyLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public EntityAssemblyLoadContext(string mainAssemblyPath)
        : base($"MicroM.APPEntities.{Path.GetFileNameWithoutExtension(mainAssemblyPath)}.{Guid.NewGuid():N}", isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path != null ? LoadFromAssemblyPath(path) : null;
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path != null ? LoadUnmanagedDllFromPath(path) : nint.Zero;
    }
}