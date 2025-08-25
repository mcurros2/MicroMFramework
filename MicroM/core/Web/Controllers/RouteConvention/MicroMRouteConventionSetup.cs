using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MicroM.Web.Controllers
{
    /// <summary>
    /// Registers the <see cref="MicroMRouteConvention"/> with MVC.
    /// </summary>
    public class MicroMRouteConventionSetup(MicroMRouteConvention routeConvention) : IConfigureOptions<MvcOptions>
    {
        private readonly MicroMRouteConvention _routeConvention = routeConvention;

        /// <summary>
        /// Adds the convention to MVC options.
        /// </summary>
        public void Configure(MvcOptions options)
        {
            options.Conventions.Add(_routeConvention);
        }
    }
}