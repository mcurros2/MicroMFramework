using Microsoft.Extensions.DependencyInjection;

namespace MicroM.Web.Services;

public static class AssemblyReloadServicesExtensions
{
    public static IServiceCollection AddAssemblyReloadInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IAssemblyShadowCopyService, AssemblyShadowCopyService>();
        services.AddSingleton<IAssemblyLoadContextService, AssemblyLoadContextService>();
        services.AddSingleton<IAssemblyFileWatcherService, AssemblyFileWatcherService>();
        services.AddSingleton<IAppAssemblyRuntimeManager, AppAssemblyRuntimeManager>();

        return services;
    }
}