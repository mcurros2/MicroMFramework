using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MicroM.Web.Services;

public interface IMicroMApplicationServices
{
    void RegisterServices(IServiceCollection services, IConfiguration configuration);
}