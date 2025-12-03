using MicroM.Configuration;
using MicroM.Web.Authentication.SSO;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MicroM.Web.Extensions;

public static class HttpClientRegistrationExtensions
{
    public static IServiceCollection AddMicroMOidcHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient(ConfigurationDefaults.HTTPClientOidcName, client =>
        {
            client.DefaultRequestVersion = HttpVersion.Version20;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
            client.DefaultRequestHeaders.AcceptCharset.Add(new StringWithQualityHeaderValue(UTF8Encoding.UTF8.WebName));
            client.DefaultRequestHeaders.UserAgent.ParseAdd(ConfigurationDefaults.HTTPClientOidcUserAgent);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            SslOptions = new SslClientAuthenticationOptions
            {
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                CertificateRevocationCheckMode = X509RevocationMode.Online
            },
            AllowAutoRedirect = true
        });

        services.AddHttpClient(ConfigurationDefaults.HTTPClientJwksName, client =>
        {
            client.DefaultRequestVersion = HttpVersion.Version20;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
            client.DefaultRequestHeaders.AcceptCharset.Add(new StringWithQualityHeaderValue(UTF8Encoding.UTF8.WebName));
            client.DefaultRequestHeaders.UserAgent.ParseAdd(ConfigurationDefaults.HTTPClientJwksUserAgent);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            SslOptions = new SslClientAuthenticationOptions
            {
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                CertificateRevocationCheckMode = X509RevocationMode.Online
            },
            AllowAutoRedirect = true
        });

        services.AddTransient<IOIDCHttpClient, OIDCHttpClient>();

        return services;
    }
}