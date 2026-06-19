using MicroM.Configuration;
using MicroM.Web.Authentication.SSO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
        static bool IsLoopbackHostNoDns(string? host)
        {
            if (string.IsNullOrWhiteSpace(host)) return false;

            if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;

            return IPAddress.TryParse(host, out var ip) && IPAddress.IsLoopback(ip);
        }

        static SocketsHttpHandler CreateHandler(IServiceProvider sp)
        {
            var config = sp.GetRequiredService<IOptions<MicroMOptions>>().Value;

            var handler = new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                AllowAutoRedirect = true,
                SslOptions = new SslClientAuthenticationOptions
                {
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    CertificateRevocationCheckMode = X509RevocationMode.Online
                }
            };

            if (!config.AllowInvalidCertificatesOnLoopback)
                return handler;

            if (config.DisableRevocationCheckWhenAllowingInvalidLoopbackCerts) handler.SslOptions.CertificateRevocationCheckMode = X509RevocationMode.NoCheck;

            handler.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
            {
                if (errors == SslPolicyErrors.None)
                    return true;

                var host = (sender as SslStream)?.TargetHostName;
                if (!IsLoopbackHostNoDns(host)) return false;

                return (errors & SslPolicyErrors.RemoteCertificateChainErrors) != 0;
            };

            return handler;
        }

        services.AddHttpClient(ConfigurationDefaults.HTTPClientOidcName, client =>
        {
            client.DefaultRequestVersion = HttpVersion.Version20;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
            client.DefaultRequestHeaders.AcceptCharset.Add(new StringWithQualityHeaderValue(UTF8Encoding.UTF8.WebName));
            client.DefaultRequestHeaders.UserAgent.ParseAdd(ConfigurationDefaults.HTTPClientOidcUserAgent);
        })
        .ConfigurePrimaryHttpMessageHandler(CreateHandler);

        services.AddHttpClient(ConfigurationDefaults.HTTPClientJwksName, client =>
        {
            client.DefaultRequestVersion = HttpVersion.Version20;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
            client.DefaultRequestHeaders.AcceptCharset.Add(new StringWithQualityHeaderValue(UTF8Encoding.UTF8.WebName));
            client.DefaultRequestHeaders.UserAgent.ParseAdd(ConfigurationDefaults.HTTPClientJwksUserAgent);
        })
        .ConfigurePrimaryHttpMessageHandler(CreateHandler);

        services.AddTransient<IOIDCHttpClient, OIDCHttpClient>();

        return services;
    }
}