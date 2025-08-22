using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MicroM.Web.Controllers
{
    public class MicroMRouteConventionSetup(MicroMRouteConvention routeConvention) : IConfigureOptions<MvcOptions>
    {
        private readonly MicroMRouteConvention _routeConvention = routeConvention;

        public void Configure(MvcOptions options)
        {
            options.Conventions.Add(_routeConvention);
        }
    }
}