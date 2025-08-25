using MicroM.Configuration;
using MicroM.Web.Authentication;
using MicroM.Web.Controllers;
using MicroM.Web.Middleware;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using System.Security.Claims;

namespace MicroM.Web.Services
{
    /// <summary>
    /// Represents the WebAPIBaseExtensions.
    /// </summary>
    public static class WebAPIBaseExtensions
    {

        /// <summary>
        /// Performs the AddDeviceIDService operation.
        /// </summary>
        public static IServiceCollection AddDeviceIDService(this IServiceCollection services)
        {
            services.AddSingleton<IDeviceIdService, BrowserDeviceIDService>();
            return services;
        }

        /// <summary>
        /// Performs the AddFileUploadService operation.
        /// </summary>
        public static IServiceCollection AddFileUploadService(this IServiceCollection services)
        {
            services.AddSingleton<IFileUploadService, FileUploadService>();
            return services;
        }

        /// <summary>
        /// Performs the AddThumbnailService operation.
        /// </summary>
        public static IServiceCollection AddThumbnailService(this IServiceCollection services)
        {
            services.AddSingleton<IThumbnailService, ImageThumbnailService>();
            return services;
        }

        /// <summary>
        /// Performs the AddAuthenticationService operation.
        /// </summary>
        public static IServiceCollection AddAuthenticationService(this IServiceCollection services)
        {
            services.AddSingleton<IAuthenticationService, AuthenticationService>();
            return services;
        }

        /// <summary>
        /// Performs the AddEntitiesService operation.
        /// </summary>
        public static IServiceCollection AddEntitiesService(this IServiceCollection services)
        {
            services.AddSingleton<IEntitiesService, EntitiesService>();
            return services;
        }

        //public static IServiceCollection AddWebAPIServices(this IServiceCollection services)
        //{
        //    services.AddSingleton<IWebAPIServices, WebAPIServices>();
        //    return services;
        //}

        /// <summary>
        /// Performs the AddMicroMEncryption operation.
        /// </summary>
        public static IServiceCollection AddMicroMEncryption(this IServiceCollection services)
        {
            services.AddSingleton<IMicroMEncryption, MicroMEncryption>();
            return services;
        }

        /// <summary>
        /// Performs the AddMicroMAuthenticator operation.
        /// </summary>
        public static IServiceCollection AddMicroMAuthenticator(this IServiceCollection services)
        {
            services.AddSingleton<IAuthenticator, MicroMAuthenticator>();
            return services;
        }

        /// <summary>
        /// Performs the AddSQLServerAuthenticator operation.
        /// </summary>
        public static IServiceCollection AddSQLServerAuthenticator(this IServiceCollection services)
        {
            services.AddSingleton<IAuthenticator, SQLServerAuthenticator>();
            return services;
        }

        /// <summary>
        /// Performs the AddMicroMAuthenticationProvider operation.
        /// </summary>
        public static IServiceCollection AddMicroMAuthenticationProvider(this IServiceCollection services)
        {
            services.AddSingleton<IAuthenticationProvider, MicroMAuthenticationProvider>();
            return services;
        }

        /// <summary>
        /// Performs the UseDebugRoutes operation.
        /// </summary>
        public static IApplicationBuilder UseDebugRoutes(this IApplicationBuilder app, string path = "/debug-routes")
        {
            return app.UseMiddleware<DebugRoutesMiddleware>(path);
        }

        /// <summary>
        /// Performs the ToClaimsDictionary operation.
        /// </summary>
        public static Dictionary<string, object> ToClaimsDictionary(this IEnumerable<Claim> claims)
        {
            return claims.GroupBy(c => c.Type)
                         .ToDictionary(
                             g => g.Key,
                             g => (object)string.Join(",", g.Select(c => c.Value))
                         );
        }


        // Hosted services
        /// <summary>
        /// Performs the AddMemoryQueue operation.
        /// </summary>
        public static IServiceCollection AddMemoryQueue(this IServiceCollection services)
        {
            services.AddSingleton<MemoryQueueHostedService>();
            services.AddHostedService(provider => provider.GetRequiredService<MemoryQueueHostedService>());
            services.AddSingleton<IBackgroundTaskQueue>(provider => provider.GetRequiredService<MemoryQueueHostedService>());

            return services;
        }

        /// <summary>
        /// Performs the AddMicroMAppConfiguration operation.
        /// </summary>
        public static IServiceCollection AddMicroMAppConfiguration(this IServiceCollection services)
        {
            services.AddSingleton<MicroMAppConfigurationProvider>();
            services.AddHostedService(provider => provider.GetRequiredService<MicroMAppConfigurationProvider>());
            services.AddSingleton<IMicroMAppConfiguration>(provider => provider.GetRequiredService<MicroMAppConfigurationProvider>());

            return services;
        }

        /// <summary>
        /// Performs the AddEmailService operation.
        /// </summary>
        public static IServiceCollection AddEmailService(this IServiceCollection services)
        {
            services.AddSingleton<EmailHostedService>();
            services.AddHostedService(provider => provider.GetRequiredService<EmailHostedService>());
            services.AddSingleton<IEmailService>(provider => provider.GetRequiredService<EmailHostedService>());

            return services;
        }

        /// <summary>
        /// Performs the AddSecurityService operation.
        /// </summary>
        public static IServiceCollection AddSecurityService(this IServiceCollection services)
        {
            services.AddSingleton<SecurityService>();
            services.AddHostedService(provider => provider.GetRequiredService<SecurityService>());
            services.AddSingleton<ISecurityService>(provider => provider.GetRequiredService<SecurityService>());

            return services;
        }

        /// <summary>
        /// Performs the AddMicroMApiServices operation.
        /// </summary>
        public static IServiceCollection AddMicroMApiServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MicroMOptions>(configuration.GetSection(MicroMOptions.MicroM));

            services.AddMemoryQueue();
            services.AddDeviceIDService();
            services.AddMicroMEncryption();
            services.AddMicroMAppConfiguration();
            services.AddMicroMAuthenticator();
            services.AddSQLServerAuthenticator();
            services.AddMicroMAuthenticationProvider();
            services.AddThumbnailService();
            services.AddFileUploadService();
            services.AddEmailService();
            services.AddSecurityService();
            services.AddAuthenticationService();
            services.AddEntitiesService();
            //services.AddWebAPIServices();

            services.AddSingleton<WebAPIJsonWebTokenHandler>();
            services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, WebAPIJwtPostConfigurationOptions>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.Configure<JsonOptions>(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

            services.AddSingleton<ICookieManager, MicroMCookieManager>();
            services.AddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>, MicroMCookiesManagerSetup>();

            services.AddTransient<MicroMRouteConvention>();
            services.ConfigureOptions<MicroMRouteConventionSetup>();

            //JWT Authentication
            const string JWT_COOKIE_POLICY = "JwtCookie";
            const string JWT_COOKIE_POLICY_DISPLAYNAME = "Jwt/Cookie";
            const string COOKIE_NAME = "microm-a";

            services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JWT_COOKIE_POLICY;
                opt.DefaultChallengeScheme = JWT_COOKIE_POLICY;
                opt.DefaultScheme = JWT_COOKIE_POLICY;
            }
            ).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                };

            })
            .AddCookie(options =>
            {
                options.Cookie.Name = COOKIE_NAME;
                options.LoginPath = string.Empty;
                options.LogoutPath = string.Empty;
                options.AccessDeniedPath = string.Empty;
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            }).AddPolicyScheme(JWT_COOKIE_POLICY, JWT_COOKIE_POLICY_DISPLAYNAME, opt =>
            {
                // This will switch between JwtToken or cookie authentication
                opt.ForwardDefaultSelector = context =>
                {
                    string? authorization = context.Request.Headers[HeaderNames.Authorization];
                    if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return "Bearer";

                    return "Cookies";
                };
            });

            // Configure cookies manager
            services.AddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>, MicroMCookiesManagerSetup>();

            services.AddAuthorizationBuilder()
                .AddPolicy(nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy), policy =>
                    policy.Requirements.Add(new MicroMPermissionsRequirement()));

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.DisableImplicitFromServicesParameters = true;
            });

            services.AddSingleton<IAuthorizationHandler, MicroMPermissionsHandler>();

            services.AddControllers();

            return services;
        }

        /// <summary>
        /// Performs the UseMicroMWebAPI operation.
        /// </summary>
        public static IApplicationBuilder UseMicroMWebAPI(this IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<PublicEndpointsMiddleware>();

            return app;
        }

    }
}
