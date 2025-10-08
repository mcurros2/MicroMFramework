using MicroM.Configuration;
using MicroM.Web.Authentication;
using MicroM.Web.Authentication.SSO;
using MicroM.Web.Controllers;
using MicroM.Web.Extensions;
using MicroM.Web.Middleware;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Authentication;
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

namespace MicroM.Web.Services;

public static class WebAPIBaseExtensions
{

    public static IServiceCollection AddDeviceIDService(this IServiceCollection services)
    {
        services.AddSingleton<IDeviceIdService, BrowserDeviceIDService>();
        return services;
    }

    public static IServiceCollection AddFileUploadService(this IServiceCollection services)
    {
        services.AddSingleton<IFileUploadService, FileUploadService>();
        return services;
    }

    public static IServiceCollection AddThumbnailService(this IServiceCollection services)
    {
        services.AddSingleton<IThumbnailService, ImageThumbnailService>();
        return services;
    }

    public static IServiceCollection AddAuthenticationService(this IServiceCollection services)
    {
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        return services;
    }

    public static IServiceCollection AddEntitiesService(this IServiceCollection services)
    {
        services.AddSingleton<IEntitiesService, EntitiesService>();
        return services;
    }

    public static IServiceCollection AddMicroMEncryption(this IServiceCollection services)
    {
        services.AddSingleton<IMicroMEncryption, MicroMEncryption>();
        return services;
    }

    public static IServiceCollection AddMicroMAuthenticator(this IServiceCollection services)
    {
        services.AddSingleton<IAuthenticator, MicroMAuthenticator>();
        return services;
    }

    public static IServiceCollection AddSQLServerAuthenticator(this IServiceCollection services)
    {
        services.AddSingleton<IAuthenticator, SQLServerAuthenticator>();
        return services;
    }

    public static IServiceCollection AddMicroMAuthenticationProvider(this IServiceCollection services)
    {
        services.AddSingleton<IAuthenticationProvider, MicroMAuthenticationProvider>();
        return services;
    }

    public static IApplicationBuilder UseDebugRoutes(this IApplicationBuilder app, string path = "/debug-routes")
    {
        return app.UseMiddleware<DebugRoutesMiddleware>(path);
    }

    public static Dictionary<string, object> ToClaimsDictionary(this IEnumerable<Claim> claims)
    {
        return claims.GroupBy(c => c.Type)
                     .ToDictionary(
                         g => g.Key,
                         g => (object)string.Join(",", g.Select(c => c.Value))
                     );
    }


    // Hosted services
    public static IServiceCollection AddMemoryQueue(this IServiceCollection services)
    {
        services.AddSingleton<MemoryQueueHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<MemoryQueueHostedService>());
        services.AddSingleton<IBackgroundTaskQueue>(provider => provider.GetRequiredService<MemoryQueueHostedService>());

        return services;
    }

    public static IServiceCollection AddMicroMAppConfiguration(this IServiceCollection services)
    {
        services.AddSingleton<MicroMAppConfigurationProvider>();
        services.AddHostedService(provider => provider.GetRequiredService<MicroMAppConfigurationProvider>());
        services.AddSingleton<IMicroMAppConfiguration>(provider => provider.GetRequiredService<MicroMAppConfigurationProvider>());

        return services;
    }

    public static IServiceCollection AddEmailService(this IServiceCollection services)
    {
        services.AddSingleton<EmailHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailHostedService>());
        services.AddSingleton<IEmailService>(provider => provider.GetRequiredService<EmailHostedService>());

        return services;
    }

    public static IServiceCollection AddSecurityService(this IServiceCollection services)
    {
        services.AddSingleton<SecurityService>();
        services.AddHostedService(provider => provider.GetRequiredService<SecurityService>());
        services.AddSingleton<ISecurityService>(provider => provider.GetRequiredService<SecurityService>());

        return services;
    }

    public static IServiceCollection AddOIDCServices(this IServiceCollection services)
    {
        services.AddSingleton<IApplicationCertificateCacheService, ApplicationCertificateCacheService>();
        services.AddSingleton<IEtagCacheService, EtagCacheService>();
        services.AddSingleton<IJwksService, JwksService>();
        services.AddSingleton<IAuthorizationCodeService, MemoryAuthorizationCodeService>();
        services.AddSingleton<IOauthTokenService, OauthTokenService>();
        services.AddSingleton<IPushedAuthorizationService, PushedAuthorizationService>();
        services.AddSingleton<IOIDCSessionService, OIDCSessionService>();
        services.AddSingleton<IOIDCClientService, OIDCClientService>();
        services.AddSingleton<IStateAndNonceService, StateAndNonceService>();

        return services;
    }

    public static IServiceCollection AddIdentityProviderService(this IServiceCollection services)
    {
        services.AddOIDCServices();
        services.AddSingleton<IIdentityProviderService, IdentityProviderService>();
        return services;
    }

    public static IServiceCollection AddMicroMApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MicroMOptions>(configuration.GetSection(MicroMOptions.MicroM));

        services.AddHttpClient();
        services.AddMemoryCache();

        services.AddMicroMOidcHttpClients();

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

        services.AddIdentityProviderService();
        services.AddSingleton<IOIDCReplayCacheService, OIDCReplayCacheService>();


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

        const string IDP_CLIENT_SCHEME = "IdPClient";
        const string IDP_CLIENT_SCHEME_DISPLAYNAME = "IdP Client auth";

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
        })
        // Add IdP client authentication handler
        .AddScheme<AuthenticationSchemeOptions, IdPBackchannelAuthenticationHandler>(IDP_CLIENT_SCHEME, IDP_CLIENT_SCHEME_DISPLAYNAME, options => { });

        // Configure cookies manager
        services.AddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>, MicroMCookiesManagerSetup>();

        services.AddAuthorizationBuilder()
            .AddPolicy(nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy), policy =>
                policy.Requirements.Add(new MicroMPermissionsRequirement()));

        // Add an authorization policy that requires IdP client authentication
        services.AddAuthorization(options =>
        {
            options.AddPolicy(nameof(MicroMPermissionsConstants.IdPClientPolicy), policy =>
            {
                policy.AddAuthenticationSchemes(IDP_CLIENT_SCHEME);
                policy.RequireAuthenticatedUser();
            });
        });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.DisableImplicitFromServicesParameters = true;
        });

        services.AddSingleton<IAuthorizationHandler, MicroMPermissionsHandler>();

        services.AddControllers();

        return services;
    }

    public static IApplicationBuilder UseMicroMWebAPI(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<PublicEndpointsMiddleware>();

        return app;
    }

}
