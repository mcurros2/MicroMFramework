﻿using MicroM.Configuration;
using MicroM.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MicroM.Web.Controllers
{
    public class MicroMRouteConvention : IApplicationModelConvention
    {
        private readonly MicroMOptions _options;
        private readonly ILogger<MicroMRouteConvention> _log;
        private readonly PathString _basePathString;

        public MicroMRouteConvention(IOptions<MicroMOptions> options, ILogger<MicroMRouteConvention> logger)
        {
            _options = options.Value;
            _log = logger;

            var basePath = _options.MicroMAPIBaseRootPath?.Trim('/') ?? string.Empty;
            _basePathString = new PathString("/" + basePath);
        }

        public void Apply(ApplicationModel application)
        {
            foreach (var controller in application.Controllers)
            {
                if (controller.ControllerType.IsIn(typeof(AuthenticationController), typeof(EntitiesController), typeof(FileController), typeof(PublicController)))
                {
                    if (string.IsNullOrEmpty(_basePathString))
                    {
                        _log.LogWarning("BasePath is empty, skipping controller {controller}", controller.ControllerType.Name);
                        return;
                    }

                    _log.LogInformation("Applying MicroMRouteConvention: root path {path} to controller {controller}", _basePathString, controller.ControllerType.Name);

                    var routeModel = new AttributeRouteModel
                    {
                        Template = _basePathString
                    };

                    controller.Selectors.Add(new SelectorModel
                    {
                        AttributeRouteModel = routeModel
                    });
                }
                else
                {
                    _log.LogWarning("Skipping controller {controller} is not an MicroM Controller Type", controller.ControllerType.Name);
                }
            }
        }

    }
}
