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
using System.Threading.RateLimiting;

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

    public static IServiceCollection AddMemoryEventBus(this IServiceCollection services)
    {
        services.AddSingleton<IMemoryEventBus, MemoryEventBus>();
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
        services.AddSingleton<IEtagCacheService<OIDCWellKnownResponse>, EtagCacheService<OIDCWellKnownResponse>>();
        services.AddSingleton<IEtagCacheService<OIDCJwksResponse>, EtagCacheService<OIDCJwksResponse>>();
        services.AddSingleton<IJwksService, JwksService>();
        services.AddSingleton<IIdPClientEncryptingCredentialsCacheService, IdPClientEncryptingCredentialsCacheService>();
        services.AddSingleton<IIdPClientSigningKeysCacheService, IdPClientSigningKeysCacheService>();
        services.AddSingleton<IAuthorizationCodeService, MemoryAuthorizationCodeService>();
        services.AddSingleton<IOauthTokenService, OauthTokenService>();
        services.AddSingleton<IPushedAuthorizationService, PushedAuthorizationService>();
        services.AddSingleton<IOIDCClientService, OIDCClientService>();
        services.AddSingleton<IStateAndNonceService, StateAndNonceService>();
        services.AddSingleton<IOIDCReplayCacheService, OIDCReplayCacheService>();

        return services;
    }

    public static IServiceCollection AddMicroMRateLimitingPolicies(this IServiceCollection services)
    {
        services.AddRateLimiter(static options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, token) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }
                await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", token);
            };

            // Partition helpers (avoid IP as primary)
            static string AppId(HttpContext ctx) =>
                ctx.Request.RouteValues.TryGetValue("app_id", out var app) ? app?.ToString() ?? "unknown" : "unknown";

            static string ClientId(HttpContext ctx) =>
                ctx.User?.FindFirst("client_id")?.Value
                ?? ctx.Request.Query["client_id"].ToString()
                ?? "unknown";

            static string DeviceId(HttpContext ctx) =>
                ctx.User?.FindFirst(MicroMServerClaimTypes.MicroMUserDeviceID)?.Value ?? "unknown";

            static string UserId(HttpContext ctx) =>
                ctx.User?.FindFirst(MicroMServerClaimTypes.MicroMUser_id)?.Value ?? "unknown";

            static string UA(HttpContext ctx) => ctx.Request.Headers.UserAgent.ToString() ?? "ua-unknown";

            // Global catch-all: per-app sliding window
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var key = $"global:app:{AppId(httpContext)}";
                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: key,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 300, // per app per minute
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 3,
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            // Authentication
            options.AddPolicy(MicroMServicesConstants.RateLimitingRefreshPolicy, httpContext =>
            {
                var device = DeviceId(httpContext);
                var key = !string.IsNullOrWhiteSpace(device)
                    ? $"refresh:app:{AppId(httpContext)}:dev:{device}"
                    : $"refresh:app:{AppId(httpContext)}:ua:{UA(httpContext)}";

                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: key,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 3,
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            options.AddPolicy(MicroMServicesConstants.RateLimitingAuthLoginPolicy, httpContext =>
            {
                var key = $"login:app:{AppId(httpContext)}:ua:{UA(httpContext)}";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: key,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 8,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            options.AddPolicy(MicroMServicesConstants.RateLimitingAuthRecoveryPolicy, httpContext =>
            {
                var key = $"recovery:app:{AppId(httpContext)}:ua:{UA(httpContext)}";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: key,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            options.AddPolicy(MicroMServicesConstants.RateLimitingAuthIsLoggedInPolicy, httpContext =>
            {
                var user = UserId(httpContext);
                var key = string.IsNullOrWhiteSpace(user) || user == "unknown"
                    ? $"isloggedin:app:{AppId(httpContext)}"
                    : $"isloggedin:app:{AppId(httpContext)}:usr:{user}";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: key,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            options.AddPolicy(MicroMServicesConstants.RateLimitingAuthLogoffPolicy, httpContext =>
            {
                var user = UserId(httpContext);
                var key = string.IsNullOrWhiteSpace(user) || user == "unknown"
                    ? $"logoff:app:{AppId(httpContext)}"
                    : $"logoff:app:{AppId(httpContext)}:usr:{user}";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: key,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            // IdP OIDC
            options.AddPolicy(MicroMServicesConstants.RateLimitingOidcPARPolicy, httpContext =>
            {
                var key = $"par:app:{AppId(httpContext)}:client:{ClientId(httpContext)}";
                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: key,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 3,
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            options.AddPolicy(MicroMServicesConstants.RateLimitingOidcTokenPolicy, httpContext =>
            {
                var key = $"token:app:{AppId(httpContext)}:client:{ClientId(httpContext)}";
                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: key,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 40,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 3,
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            options.AddPolicy(MicroMServicesConstants.RateLimitingOidcAuthorizePolicy, httpContext =>
            {
                var key = $"authz:app:{AppId(httpContext)}:client:{ClientId(httpContext)}:ua:{UA(httpContext)}";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: key,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            options.AddPolicy(MicroMServicesConstants.RateLimitingOidcEndSessionPolicy, httpContext =>
            {
                var key = $"endsession:app:{AppId(httpContext)}:client:{ClientId(httpContext)}";
                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: key,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 3,
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            options.AddPolicy(MicroMServicesConstants.RateLimitingOidcMetadataPolicy, httpContext =>
            {
                var key = $"meta:app:{AppId(httpContext)}";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: key,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            // RP logout
            options.AddPolicy(MicroMServicesConstants.RateLimitingBackchannelLogoutPolicy, httpContext =>
            {
                var key = $"backlogout:app:{AppId(httpContext)}";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: key,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            options.AddPolicy(MicroMServicesConstants.RateLimitingFrontchannelLogoutPolicy, httpContext =>
            {
                var key = $"frontlogout:app:{AppId(httpContext)}:ua:{UA(httpContext)}";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: key,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            // Public endpoints
            options.AddPolicy(MicroMServicesConstants.RateLimitingPublicGetPolicy, httpContext =>
            {
                var key = $"public:get:app:{AppId(httpContext)}:ua:{UA(httpContext)}";
                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: key,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 3,
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            options.AddPolicy(MicroMServicesConstants.RateLimitingPublicMutationPolicy, httpContext =>
            {
                var key = $"public:mut:app:{AppId(httpContext)}:ua:{UA(httpContext)}";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: key,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });
        });
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

        services.AddMemoryEventBus();
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

        // Register application-specific service configurators discovered via IMicroMApplicationServices
        using (var tmp = services.BuildServiceProvider())
        {
            var appConfig = tmp.GetRequiredService<IMicroMAppConfiguration>();
            services.AddMicroMApplicationServices(appConfig, configuration);
        }

        // Register rate limiting policies
        services.AddMicroMRateLimitingPolicies();

        services.AddIdentityProviderService();

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

        services.AddAuthentication(opt =>
        {
            opt.DefaultAuthenticateScheme = MicroMServicesConstants.JWTCookiePolicy;
            opt.DefaultChallengeScheme = MicroMServicesConstants.JWTCookiePolicy;
            opt.DefaultScheme = MicroMServicesConstants.JWTCookiePolicy;
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
            options.Cookie.Name = MicroMServicesConstants.AuthenticationCookieName;
            options.LoginPath = string.Empty;
            options.LogoutPath = string.Empty;
            options.AccessDeniedPath = string.Empty;
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        }).AddPolicyScheme(MicroMServicesConstants.JWTCookiePolicy, MicroMServicesConstants.JWTCookiePolicyDisplayName, opt =>
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
        .AddScheme<AuthenticationSchemeOptions, IdPBackchannelAuthenticationHandler>(MicroMServicesConstants.IdPClientScheme, MicroMServicesConstants.IdPClientSchemeDisplayName, options => { });

        // Configure cookies manager
        services.AddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>, MicroMCookiesManagerSetup>();

        services.AddAuthorizationBuilder()
            .AddPolicy(nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy), policy =>
                policy.Requirements.Add(new MicroMPermissionsRequirement())
                )
            // Add an authorization policy that requires IdP client authentication
            .AddPolicy(nameof(MicroMPermissionsConstants.IdPClientPolicy), policy =>
            {
                policy.AddAuthenticationSchemes(MicroMServicesConstants.IdPClientScheme);
                policy.RequireAuthenticatedUser();
            });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.DisableImplicitFromServicesParameters = true;
        });

        services.AddSingleton<IAuthorizationHandler, MicroMAuthorizationHandler>();

        services.AddControllers();

        return services;
    }

    public static IApplicationBuilder UseMicroMWebAPI(this IApplicationBuilder app)
    {
        app.UseAuthentication();

        // Ensure rate-limiting middleware is active after auth (claims available) and before authorization
        app.UseRateLimiter();

        app.UseAuthorization();
        app.UseMiddleware<PublicEndpointsMiddleware>();

        return app;
    }

}
