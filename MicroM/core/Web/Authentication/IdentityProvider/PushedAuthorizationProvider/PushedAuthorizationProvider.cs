using MicroM.Configuration;
using MicroM.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MicroM.Web.Authentication.SSO;

public static class PushedAuthorizationProvider
{
    private const int MAX_REQUEST_OBJECT_BYTES = 64 * 1024;

    public static OIDCSigningAlg? SelectAssertionAlg(X509Certificate2 cert, IReadOnlyList<OIDCSigningAlg>? supported)
    {
        if (supported == null || supported.Count == 0) return null;
        if (cert.GetECDsaPrivateKey() != null)
        {
            if (supported.Contains(OIDCSigningAlg.ES512)) return OIDCSigningAlg.ES512;
            if (supported.Contains(OIDCSigningAlg.ES384)) return OIDCSigningAlg.ES384;
            if (supported.Contains(OIDCSigningAlg.ES256)) return OIDCSigningAlg.ES256;
            return null;
        }

        OIDCSigningAlg[] preference = [OIDCSigningAlg.PS512, OIDCSigningAlg.PS384, OIDCSigningAlg.PS256, OIDCSigningAlg.RS512, OIDCSigningAlg.RS384, OIDCSigningAlg.RS256];
        foreach (var p in preference)
        {
            if (supported.Contains(p)) return p;
        }

        return null;
    }

    public static ResultWithStatus<(PushedAuthorizationRequest? request, JWTProtectedHeaderResult? header), ErrorResult>
        ValidateRequest(ApplicationOption app, IFormCollection form)
    {

        form.TryGetValue(WellknownIdentityConstants.Request, out var requestVal);

        form.TryGetValue(WellknownIdentityConstants.ClientId, out var clientIdVal);
        form.TryGetValue(WellknownIdentityConstants.ResponseType, out var responseTypeVal);
        form.TryGetValue(WellknownIdentityConstants.RedirectUri, out var redirectUriVal);
        form.TryGetValue(WellknownIdentityConstants.Scope, out var scopeVal);
        form.TryGetValue(WellknownIdentityConstants.State, out var stateVal);
        form.TryGetValue(WellknownIdentityConstants.Nonce, out var nonceVal);
        form.TryGetValue(WellknownIdentityConstants.CodeChallenge, out var codeChallengeVal);
        form.TryGetValue(WellknownIdentityConstants.CodeChallengeMethod, out var codeChallengeMethodVal);


        var request = requestVal.ToString();
        var redirectUri = redirectUriVal.ToString();
        var scope = scopeVal.ToString();
        var state = stateVal.ToString();
        var nonce = nonceVal.ToString();
        var codeChallenge = codeChallengeVal.ToString();
        var codeChallengeMethod = codeChallengeMethodVal.ToString();
        var clientId = clientIdVal.ToString();
        var responseType = responseTypeVal.ToString();

        if (!string.IsNullOrEmpty(request) &&
               (
               !string.IsNullOrEmpty(clientId) ||
               !string.IsNullOrEmpty(responseType) ||
               !string.IsNullOrEmpty(redirectUri) ||
               !string.IsNullOrEmpty(scope) ||
               !string.IsNullOrEmpty(state) ||
               !string.IsNullOrEmpty(nonce) ||
               !string.IsNullOrEmpty(codeChallenge) ||
               !string.IsNullOrEmpty(codeChallengeMethod)
               )
           )
        {
            return new((null, null), new("invalid_request", "Both 'request' parameter and individual parameters are present"));
        }

        if (!string.IsNullOrEmpty(request))
        {
            if (request.Length > MAX_REQUEST_OBJECT_BYTES)
            {
                return new((null, null), new("invalid_request", "request object exceeds size limit"));
            }

            // Basic header validation (works for both JWS and JWE)
            try
            {
                var header = JwksProvider.TryReadProtectedHeader(request);

                if (!string.IsNullOrEmpty(header.ParseError))
                {
                    return new((null, null), new("invalid_request_object", $"Request object header parse error: {header.ParseError}"));
                }

                if (header.IsJwe)
                {
                    // JWE request object: clients encrypt TO the IdP.
                    // Check key management and content encryption algorithms
                    // against what the IdP is willing to accept.
                    if (string.IsNullOrWhiteSpace(header.Alg) ||
                        !OIDCCryptoCapabilities.Idp.AllowedRequestObjectKeyManagementAlgStrings.Contains(header.Alg))
                    {
                        return new((null, null), new("invalid_request_object", $"Unsupported request object encryption alg: {header.Alg ?? "<null>"}"));
                    }

                    if (string.IsNullOrWhiteSpace(header.Enc) ||
                        !OIDCCryptoCapabilities.Idp.AllowedRequestObjectContentEncryptionAlgStrings.Contains(header.Enc))
                    {
                        return new((null, null), new("invalid_request_object", $"Unsupported request object encryption enc: {header.Enc ?? "<null>"}"));
                    }

                }
                else
                {
                    // JWS (non-encrypted) request object.
                    var alg = header.Alg;
                    if (string.IsNullOrWhiteSpace(alg))
                    {
                        return new((null, null), new("invalid_request_object", "Missing alg in request object"));
                    }

                    // Enforce allowed signing algorithms (no "none" / HS*)
                    if (alg.Equals("none", StringComparison.OrdinalIgnoreCase) ||
                        alg.StartsWith("HS", StringComparison.OrdinalIgnoreCase))
                    {
                        return new((null, null), new("invalid_request_object", "Unsupported request object signing algorithm"));
                    }

                    OIDCSigningAlg? requestObjAlg = alg switch
                    {
                        "RS256" => OIDCSigningAlg.RS256,
                        "RS384" => OIDCSigningAlg.RS384,
                        "RS512" => OIDCSigningAlg.RS512,
                        "PS256" => OIDCSigningAlg.PS256,
                        "PS384" => OIDCSigningAlg.PS384,
                        "PS512" => OIDCSigningAlg.PS512,
                        "ES256" => OIDCSigningAlg.ES256,
                        "ES384" => OIDCSigningAlg.ES384,
                        "ES512" => OIDCSigningAlg.ES512,
                        _ => null
                    };

                    if (requestObjAlg == null)
                    {
                        return new((null, null), new("invalid_request_object", $"Unrecognized or unsupported request object alg: {alg}"));
                    }

                    // Restrict to IdP capabilities for request objects / client assertions.
                    if (!OIDCCryptoCapabilities.Idp.AcceptedClientAssertionSigningAlgs.Contains(requestObjAlg.Value))
                    {
                        return new((null, null), new("invalid_request_object", "Request object signing algorithm not allowed by IdP policy"));
                    }
                }

                var result = new PushedAuthorizationRequest(
                    client_id: null,
                    response_type: null,
                    redirect_uri: null,
                    scope: null,
                    state: null,
                    nonce: null,
                    code_challenge: null,
                    code_challenge_method: null,
                    request: request
                );

                return new((result, header), null);
            }
            catch
            {
                return new((null, null), new("invalid_request_object", "Malformed request object"));
            }
        }

        // Plain PAR fields validation
        if (string.IsNullOrEmpty(responseType) || responseType != WellknownIdentityConstants.Code)
            return new((null, null), new("invalid_request", "response_type must be 'code'"));

        if (string.IsNullOrEmpty(clientId))
            return new((null, null), new("invalid_request", "client_id is required"));

        if (string.IsNullOrEmpty(redirectUri))
            return new((null, null), new("invalid_request", "redirect_uri is required"));

        if (string.IsNullOrEmpty(scope))
            return new((null, null), new("invalid_request", "scope is required"));

        if (string.IsNullOrEmpty(codeChallenge) || string.IsNullOrEmpty(codeChallengeMethod))
            return new((null, null), new("invalid_request", "PKCE code_challenge and code_challenge_method are required"));

        bool allowPlain = app.OIDCAllowPkcePlain;
        if (codeChallengeMethod != "S256" && !(allowPlain && codeChallengeMethod == "plain"))
            return new((null, null), new("invalid_request", "Unsupported code_challenge_method"));

        var plainResult = new PushedAuthorizationRequest(
            client_id: clientId,
            response_type: responseType,
            redirect_uri: redirectUri,
            scope: scope,
            state: state ?? "",
            nonce: nonce ?? "",
            code_challenge: codeChallenge ?? "",
            code_challenge_method: codeChallengeMethod ?? "",
            request: null
        );
        return new((plainResult, null), null);


    }

    public static string BuildClientAssertion(X509Certificate2 cert, string clientId, string audience, OIDCSigningAlg? signingAlg)
    {
        var now = DateTimeOffset.UtcNow;

        // Choose algorithm based on override and certificate type
        SigningCredentials creds;
        if (cert.GetECDsaPrivateKey() != null)
        {
            var ecAlg = signingAlg switch
            {
                OIDCSigningAlg.ES512 => SecurityAlgorithms.EcdsaSha512,
                OIDCSigningAlg.ES384 => SecurityAlgorithms.EcdsaSha384,
                _ => SecurityAlgorithms.EcdsaSha256
            };
            creds = new SigningCredentials(new X509SecurityKey(cert), ecAlg);
        }
        else
        {
            // RSA or others
            var rsaAlg = signingAlg switch
            {
                OIDCSigningAlg.PS512 => SecurityAlgorithms.RsaSsaPssSha512,
                OIDCSigningAlg.PS384 => SecurityAlgorithms.RsaSsaPssSha384,
                OIDCSigningAlg.PS256 => SecurityAlgorithms.RsaSsaPssSha256,
                OIDCSigningAlg.RS512 => SecurityAlgorithms.RsaSha512,
                OIDCSigningAlg.RS384 => SecurityAlgorithms.RsaSha384,
                _ => SecurityAlgorithms.RsaSha256
            };
            creds = new SigningCredentials(new X509SecurityKey(cert), rsaAlg);
        }


        var handler = new JsonWebTokenHandler();
        var desc = new SecurityTokenDescriptor
        {
            Issuer = clientId,
            Audience = audience,
            Subject = new ClaimsIdentity(
            [
                new Claim(WellknownIdentityConstants.SubjectIdentifier, clientId),
                new Claim(WellknownIdentityConstants.JWTID, Guid.NewGuid().ToString("N"))
            ]),
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

        if (!forward.TryGetValue(WellknownIdentityConstants.ResponseType, out var response_type) ||
             response_type != WellknownIdentityConstants.Code)
            return (null, new { error = "invalid_request", error_description = "response_type must be 'code'" });

        if (!forward.TryGetValue(WellknownIdentityConstants.ClientId, out var clientId) ||
            string.IsNullOrWhiteSpace(clientId) || clientId != client_app.ApplicationID)
            return (null, new { error = "invalid_request", error_description = "Invalid client_id" });

        if (!forward.TryGetValue(WellknownIdentityConstants.RedirectUri, out var redirectUri) ||
            string.IsNullOrWhiteSpace(redirectUri))
            return (null, new { error = "invalid_request", error_description = "redirect_uri is required" });

        if (!forward.TryGetValue(WellknownIdentityConstants.Scope, out var scope) ||
            string.IsNullOrWhiteSpace(scope))
            return (null, new { error = "invalid_request", error_description = "scope is required" });

        if (!forward.TryGetValue(WellknownIdentityConstants.CodeChallenge, out var codeChallenge) ||
            string.IsNullOrWhiteSpace(codeChallenge))
            return (null, new { error = "invalid_request", error_description = "code_challenge is required" });

        if (!forward.TryGetValue(WellknownIdentityConstants.CodeChallengeMethod, out var codeChallengeMethod) ||
            string.IsNullOrWhiteSpace(codeChallengeMethod))
            return (null, new { error = "invalid_request", error_description = "code_challenge_method is required" });

        bool allowPlain = client_app.OIDCAllowPkcePlain;
        if (codeChallengeMethod != "S256" && !(allowPlain && codeChallengeMethod == "plain"))
            return (null, new { error = "invalid_request", error_description = "Unsupported code_challenge_method" });

        forward.TryGetValue(WellknownIdentityConstants.State, out var state);
        if (string.IsNullOrEmpty(state))
            return (null, new { error = "invalid_request", error_description = "state is required" });

        forward.TryGetValue(WellknownIdentityConstants.Nonce, out var nonce);
        if (string.IsNullOrEmpty(nonce))
            return (null, new { error = "invalid_request", error_description = "nonce is required" });

        if (!IsRedirectUriAllowed(client_app, clientId, redirectUri))
            return (null, new { error = "invalid_request", error_description = "redirect_uri is not registered for this client" });

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
                if (!string.IsNullOrEmpty(u) && u == redirectUri)
                    return true;
            }
        }
        else
        {
            // Fallback: search by ClientAPPID field if dictionary keys differ from client_id
            foreach (var kvp in app.OIDCClientConfiguration)
            {
                var c = kvp.Value;
                if (c?.ClientAPPID != null && c.ClientAPPID == clientId && c.URLAuthorizedRedirects != null)
                {
                    foreach (var u in c.URLAuthorizedRedirects)
                    {
                        if (!string.IsNullOrEmpty(u) && u == redirectUri)
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
        IReadOnlyList<OIDCTokenEndpointAuthMethod>? supported_auth_methods,
        IReadOnlyList<OIDCSigningAlg>? supported_auth_algs
        )
    {
        bool allowBasic = supported_auth_methods == null || supported_auth_methods.Count == 0 || supported_auth_methods.Contains(OIDCTokenEndpointAuthMethod.client_secret_basic);
        bool allowPrivateKeyJwt = supported_auth_methods == null || supported_auth_methods.Count == 0 || supported_auth_methods.Contains(OIDCTokenEndpointAuthMethod.private_key_jwt);

        if (!allowBasic && !allowPrivateKeyJwt)
        {
            return new(null, "No supported client authentication methods advertised by IdP.");
        }

        var clientId = valid_form[WellknownIdentityConstants.ClientId];
        if (string.IsNullOrEmpty(clientId))
        {
            return new(null, "client_id is required");
        }

        if (allowPrivateKeyJwt && cert != null)
        {
            var chosenAlg = SelectAssertionAlg(cert, supported_auth_algs);
            string clientAssertion = BuildClientAssertion(cert, clientId, par_endpoint, chosenAlg);
            valid_form[WellknownIdentityConstants.ClientAssertionType] = WellknownIdentityConstants.ClientAssertionTypeJwtBearer;
            valid_form[WellknownIdentityConstants.ClientAssertion] = clientAssertion;

            // Remove weaker auth artifacts if present
            valid_form.Remove(WellknownIdentityConstants.ClientSecret);

            // WARNING, REVIEW THIS IS CORRECT: No Authorization header required for private_key_jwt
            return new(null, null);
        }

        if (allowBasic) // secret
        {
            // client_secret_post -> convert to Basic
            if (valid_form.TryGetValue("client_secret", out var clientSecret) && !string.IsNullOrEmpty(clientSecret))
            {
                var raw = $"{clientId}:{clientSecret}";
                var basic = Convert.ToBase64String(Encoding.ASCII.GetBytes(raw));
                var authHeader = new AuthenticationHeaderValue(WellknownIdentityConstants.Basic, basic);
                valid_form.Remove(WellknownIdentityConstants.ClientSecret);
                return new(authHeader, null);
            }

            // Pass-through Basic header if present (only in Basic mode)
            if (headers.TryGetValue(HeaderNames.Authorization, out var authValues))
            {
                var incomingAuth = authValues.ToString();
                if (!string.IsNullOrWhiteSpace(incomingAuth) &&
                    AuthenticationHeaderValue.TryParse(incomingAuth, out var parsed) &&
                    string.Equals(parsed.Scheme, WellknownIdentityConstants.Basic, StringComparison.OrdinalIgnoreCase))
                {
                    return new(parsed, null);
                }
            }

            return new(null, "client_secret or Authorization: Basic header required for client_secret_basic");
        }

        return new(null, "Client authentication not allowed by IdP metadata (private_key_jwt required and no certificate is configured).");
    }

    public static Dictionary<string, string> BuildTokenExchangeFormPrivateKeyJwt
        (
           X509Certificate2 cert,
           string clientId,
           string tokenEndpoint,
           string code,
           string redirectUri,
           string codeVerifier,
           IReadOnlyList<OIDCSigningAlg>? supported_algs
        )
    {
        var signingAlg = SelectAssertionAlg(cert, supported_algs);
        var assertion = BuildClientAssertion(cert, clientId, tokenEndpoint, signingAlg);
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [WellknownIdentityConstants.GrantType] = WellknownIdentityConstants.AuthorizationCode,
            [WellknownIdentityConstants.Code] = code,
            [WellknownIdentityConstants.RedirectUri] = redirectUri,
            [WellknownIdentityConstants.CodeVerifier] = codeVerifier,
            [WellknownIdentityConstants.ClientId] = clientId,
            [WellknownIdentityConstants.ClientAssertionType] = WellknownIdentityConstants.ClientAssertionTypeJwtBearer,
            [WellknownIdentityConstants.ClientAssertion] = assertion
        };
    }

    public static Dictionary<string, string> BuildRefreshTokenFormPrivateKeyJwt
        (
           X509Certificate2 cert,
           string clientId,
           string tokenEndpoint,
           string refreshToken,
           IReadOnlyList<OIDCSigningAlg>? supported_algs
        )
    {
        var signingAlg = SelectAssertionAlg(cert, supported_algs);
        var assertion = BuildClientAssertion(cert, clientId, tokenEndpoint, signingAlg);
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [WellknownIdentityConstants.GrantType] = WellknownIdentityConstants.RefreshToken,
            [WellknownIdentityConstants.RefreshToken] = refreshToken,
            [WellknownIdentityConstants.ClientId] = clientId,
            [WellknownIdentityConstants.ClientAssertionType] = WellknownIdentityConstants.ClientAssertionTypeJwtBearer,
            [WellknownIdentityConstants.ClientAssertion] = assertion
        };
    }

}
