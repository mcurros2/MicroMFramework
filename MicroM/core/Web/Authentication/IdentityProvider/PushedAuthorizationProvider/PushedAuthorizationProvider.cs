using MicroM.Configuration;
using MicroM.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MicroM.Web.Authentication.SSO;

public static class PushedAuthorizationProvider
{
    public static (PushedAuthorizationRequest? request, object? error) ValidateRequest(IFormCollection form)
    {
        var responseType = form["response_type"].ToString();
        var redirectUri = form["redirect_uri"].ToString();
        var scope = form["scope"].ToString();
        var state = form["state"].ToString();
        var codeChallenge = form["code_challenge"].ToString();
        var codeChallengeMethod = form["code_challenge_method"].ToString();
        var clientId = form["client_id"].ToString();
        if (string.IsNullOrEmpty(responseType) || !string.Equals(responseType, "code", StringComparison.OrdinalIgnoreCase))
        {

            return (null, new { error = "invalid_request", error_description = "response_type must be 'code'" });

        }
        if (string.IsNullOrEmpty(redirectUri))
        {
            return (null, new { error = "invalid_request", error_description = "redirect_uri is required" });
        }
        if (string.IsNullOrEmpty(scope))
        {
            return (null, new { error = "invalid_request", error_description = "scope is required" });
        }
        var result = new PushedAuthorizationRequest(
            client_id: clientId,
            response_type: responseType,
            redirect_uri: redirectUri,
            scope: scope,
            state: state ?? "",
            code_challenge: codeChallenge ?? "",
            code_challenge_method: codeChallengeMethod ?? ""
        );
        return (result, null);
    }

    public static string GenerateBase64UrlCode(int sizeBytes)
    {
        var bytes = RandomNumberGenerator.GetBytes(sizeBytes);
        return Base64UrlEncoder.Encode(bytes);
    }

    public static string BuildClientAssertion(X509Certificate2 cert, string clientId, string audience)
    {
        var now = DateTimeOffset.UtcNow;
        var creds = new X509SigningCredentials(cert); // defaults to RS256 with RSA cert

        var handler = new JsonWebTokenHandler();
        var desc = new SecurityTokenDescriptor
        {
            Issuer = clientId,
            Audience = audience,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", clientId),
                new Claim("jti", Guid.NewGuid().ToString("N"))
            }),
            NotBefore = now.UtcDateTime,
            Expires = now.AddMinutes(5).UtcDateTime,
            IssuedAt = now.UtcDateTime,
            SigningCredentials = creds
        };

        return handler.CreateToken(desc);
    }

    public static (Dictionary<string, string>? valid_form, object? error) ValidateSignInForm(ApplicationOption client_app, IFormCollection form)
    {
        var forward = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var k in form.Keys)
        {
            forward[k] = form[k].ToString();
        }

        forward.TryGetValue("response_type", out var response_type);
        if (string.IsNullOrEmpty(response_type) || response_type != "code")
        {
            return (null, new { error = "invalid_request", error_description = "response_type must be 'code'" });
        }

        // Validate required parameters
        if (!forward.TryGetValue("client_id", out var clientId) || string.IsNullOrWhiteSpace(clientId) || clientId != client_app.ApplicationID)
        {
            return (null, new { error = "invalid_request", error_description = "Invalid client_id" });
        }

        if (!forward.TryGetValue("redirect_uri", out var redirectUri) || string.IsNullOrWhiteSpace(redirectUri))
        {
            return (null, new { error = "invalid_request", error_description = "redirect_uri is required" });
        }

        if (!IsRedirectUriAllowed(client_app, clientId, redirectUri))
        {
            return (null, new { error = "invalid_request", error_description = "redirect_uri is not registered for this client" });
        }

        return (forward, null);

    }

    public static bool IsRedirectUriAllowed(ApplicationOption app, string clientId, string redirectUri)
    {
        if (string.IsNullOrWhiteSpace(redirectUri) || app.OIDCClientConfiguration == null)
            return false;

        // Require exact match by spec
        if (app.OIDCClientConfiguration.TryGetValue(clientId, out var cfg) && cfg?.URLAuthorizedRedirects != null)
        {
            foreach (var u in cfg.URLAuthorizedRedirects)
            {
                if (!string.IsNullOrEmpty(u) && string.Equals(u, redirectUri, StringComparison.Ordinal))
                    return true;
            }
        }
        else
        {
            // Fallback: search by ClientAPPID field if dictionary keys differ from client_id
            foreach (var kvp in app.OIDCClientConfiguration)
            {
                var c = kvp.Value;
                if (c?.ClientAPPID != null &&
                    string.Equals(c.ClientAPPID, clientId, StringComparison.Ordinal) &&
                    c.URLAuthorizedRedirects != null)
                {
                    foreach (var u in c.URLAuthorizedRedirects)
                    {
                        if (!string.IsNullOrEmpty(u) && string.Equals(u, redirectUri, StringComparison.Ordinal))
                            return true;
                    }
                }
            }
        }

        return false;
    }

    public static ResultWithStatus<AuthenticationHeaderValue, string> GetClientAuthorizationHeader
        (
        IHeaderDictionary headers,
        X509Certificate2? cert,
        string par_endpoint,
        Dictionary<string, string> valid_form,
        List<OIDCTokenEndpointAuthMethod>? supported_auth_methods
        )
    {
        bool allowBasic = supported_auth_methods == null || supported_auth_methods.Count == 0 || supported_auth_methods.Contains(OIDCTokenEndpointAuthMethod.client_secret_basic);
        bool allowPrivateKeyJwt = supported_auth_methods == null || supported_auth_methods.Count == 0 || supported_auth_methods.Contains(OIDCTokenEndpointAuthMethod.private_key_jwt);

        if (!allowBasic && !allowPrivateKeyJwt)
        {
            return new(null, "No supported client authentication methods advertised by IdP.");
        }

        //AuthenticationHeaderValue? authHeader = null;
        var clientId = valid_form["client_id"];
        if (string.IsNullOrEmpty(clientId))
        {
            return new(null, "client_id is required");
        }

        // private_key_jwt
        if (allowPrivateKeyJwt && cert != null)
        {
            string clientAssertion = BuildClientAssertion(cert, clientId, par_endpoint);
            valid_form["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
            valid_form["client_assertion"] = clientAssertion;

            // Remove weaker auth artifacts if present
            valid_form.Remove("client_secret");

            // No Authorization header required for private_key_jwt
            return new(null, null);
        }

        if (allowBasic) // secret
        {
            // client_secret_post -> convert to Basic
            if (valid_form.TryGetValue("client_secret", out var clientSecret) && !string.IsNullOrEmpty(clientSecret))
            {
                var raw = $"{clientId}:{clientSecret}";
                var basic = Convert.ToBase64String(Encoding.ASCII.GetBytes(raw));
                var authHeader = new AuthenticationHeaderValue("Basic", basic);
                valid_form.Remove("client_secret");
                return new(authHeader, null);
            }

            // Pass-through Basic header if present (only in Basic mode)
            if (headers.TryGetValue("Authorization", out var authValues))
            {
                var incomingAuth = authValues.ToString();
                if (!string.IsNullOrWhiteSpace(incomingAuth) &&
                    AuthenticationHeaderValue.TryParse(incomingAuth, out var parsed) &&
                    string.Equals(parsed.Scheme, "Basic", StringComparison.OrdinalIgnoreCase))
                {
                    return new(parsed, null);
                }
            }

            return new(null, "client_secret or Authorization: Basic header required for client_secret_basic");
        }

        // If we get here, private_key_jwt was required but cert was not available
        return new(null, "Client authentication not allowed by IdP metadata (private_key_jwt required and no certificate is configured).");
    }

    public static Dictionary<string, string> BuildTokenExchangeFormPrivateKeyJwt
        (
           X509Certificate2 cert,
           string clientId,
           string tokenEndpoint,
           string code,
           string redirectUri,
           string codeVerifier
        )
    {
        var assertion = BuildClientAssertion(cert, clientId, tokenEndpoint);
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = codeVerifier,
            ["client_id"] = clientId,
            ["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
            ["client_assertion"] = assertion
        };
    }

}
