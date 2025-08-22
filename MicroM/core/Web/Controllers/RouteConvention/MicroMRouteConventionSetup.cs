using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MicroM.Web.Controllers
{
    /// <summary>
    /// Represents the MicroMRouteConventionSetup.
    /// </summary>
    public class MicroMRouteConventionSetup(MicroMRouteConvention routeConvention) : IConfigureOptions<MvcOptions>
    {
        private readonly MicroMRouteConvention _routeConvention = routeConvention;

        /// <summary>
        /// Performs the Configure operation.
        /// </summary>
        public void Configure(MvcOptions options)
        {
            options.Conventions.Add(_routeConvention);
        }
    }
}