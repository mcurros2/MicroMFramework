using MicroM.Configuration;
using MicroM.Web.Services;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace MicroM.Web.Authentication.SSO;

public class IdentityProviderService : IIdentityProviderService
{
    private readonly ConcurrentDictionary<string, X509Certificate2> _certificateCache = new();

    private EtagCache _etagCache = new();

    private JsonSerializerOptions _jsonSerializationOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private JsonSerializerOptions _jsonUnsafeSerializationOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };


    private X509Certificate2 AddCertificateToCache(ApplicationOption app)
    {
        if (app.OIDCCertificateBlob == null || app.OIDCCertificateBlob.Length == 0) throw new InvalidOperationException("OIDC Certificate Blob is null or empty");
        return _certificateCache.GetOrAdd(app.ApplicationID, key =>
        {
            var cert = new X509Certificate2(app.OIDCCertificateBlob, app.OIDCCertificatePassword, X509KeyStorageFlags.EphemeralKeySet);
            _certificateCache[app.ApplicationID] = cert;
            return cert;
        });
    }

    public void ClearCache()
    {
        _certificateCache.Clear();
        _etagCache.Clear();
    }

    private OIDCWellKnownResponse CreateWellKnown(ApplicationOption app, string request_base)
    {
        return new OIDCWellKnownResponse
        {
            issuer = request_base,
            authorization_endpoint = $"{request_base}/oauth2/authorize",
            token_endpoint = $"{request_base}/oauth2/token",
            userinfo_endpoint = $"{request_base}/oauth2/userinfo",
            jwks_uri = $"{request_base}/oidc/jwks",
            pushed_authorization_request_endpoint = $"{request_base}/oauth2/par",
            end_session_endpoint = $"{request_base}/oauth2/endsession",
            revocation_endpoint = $"{request_base}/oauth2/revoke",
            introspection_endpoint = $"{request_base}/oauth2/introspect",

            // Capabilities
            response_types_supported = [OIDCResponseType.code],
            response_modes_supported = [OIDCResponseMode.query, OIDCResponseMode.form_post],
            subject_types_supported = [OIDCSubjectType.@public],
            id_token_signing_alg_values_supported = [OIDCSigningAlg.RS256, app.OIDCTokenSigningAlg!.Value],
            code_challenge_methods_supported = [app.OIDCTokenCodeChallengeMethod!.Value],
            grant_types_supported = [OIDCGrantType.authorization_code, OIDCGrantType.refresh_token],
            backchannel_logout_supported = true,
            backchannel_logout_session_supported = true,

            require_pushed_authorization_requests = true,
            request_uri_parameter_supported = true,
            authorization_response_iss_parameter_supported = true,

            introspection_endpoint_auth_methods_supported = [OIDCTokenEndpointAuthMethod.private_key_jwt],
            introspection_endpoint_auth_signing_alg_values_supported = [OIDCSigningAlg.RS256, app.OIDCTokenSigningAlg!.Value],

            revocation_endpoint_auth_methods_supported = [OIDCTokenEndpointAuthMethod.private_key_jwt],
            revocation_endpoint_auth_signing_alg_values_supported = [OIDCSigningAlg.RS256, app.OIDCTokenSigningAlg!.Value],

            // Token endpoint
            token_endpoint_auth_methods_supported = [OIDCTokenEndpointAuthMethod.private_key_jwt],
            token_endpoint_auth_signing_alg_values_supported = [OIDCSigningAlg.RS256, app.OIDCTokenSigningAlg!.Value],

            scopes_supported = [OIDCProfileScopes.openid, OIDCProfileScopes.profile, OIDCProfileScopes.email]
        };
    }

    public EtagContent HandleWellKnown(ApplicationOption app, string request_base, CancellationToken ct)
    {
        var key = $"{app.ApplicationID}_WK";
        var result = _etagCache.Get(key);

        result ??= _etagCache.GetOrAdd(key, () =>
            {
                var wellKnown = CreateWellKnown(app, request_base);
                return JsonSerializer.Serialize(wellKnown, _jsonSerializationOptions);
            });

        return result;
    }

    private OIDCJwksKeyResponse? GetRSAKey(ApplicationOption app, X509Certificate2 cert)
    {
        var kid = !string.IsNullOrEmpty(app.OIDCCertificateUniqueID) ? app.OIDCCertificateUniqueID : cert.Thumbprint;
        var x5c = new List<string> { Convert.ToBase64String(cert.RawData) };

        var sha1 = SHA1.HashData(cert.RawData);
        var sha256 = SHA256.HashData(cert.RawData);
        var x5t = Base64UrlEncoder.Encode(sha1);
        var x5tS256 = Base64UrlEncoder.Encode(sha256);

        using var rsa = cert.GetRSAPublicKey();
        if (rsa != null)
        {
            var par = rsa.ExportParameters(false);

            var rsaKey = new OIDCJwksKeyResponse
            {
                kid = kid,
                kty = OIDCKeyType.RSA,
                use = OIDCKeyUse.sig,
                // MMC: omited alg here to allow both RS256 and RS512 with the same certificate
                // Setting alg here would make clients compare the alg with well knwon
                //alg = app.OIDCTokenSigningAlg,
                n = Base64UrlEncoder.Encode(par.Modulus),
                e = Base64UrlEncoder.Encode(par.Exponent),
                x5c = x5c,
                x5t = x5t,
                x5tS256 = x5tS256
            };

            return rsaKey;
        }

        return null;
    }

    private OIDCJwksKeyResponse? GetECDKey(ApplicationOption app, X509Certificate2 cert)
    {
        var kid = !string.IsNullOrEmpty(app.OIDCCertificateUniqueID) ? app.OIDCCertificateUniqueID : cert.Thumbprint;
        var x5c = new List<string> { Convert.ToBase64String(cert.RawData) };

        var sha1 = SHA1.HashData(cert.RawData);
        var sha256 = SHA256.HashData(cert.RawData);
        var x5t = Base64UrlEncoder.Encode(sha1);
        var x5tS256 = Base64UrlEncoder.Encode(sha256);

        using var ecdsa = cert.GetECDsaPublicKey();
        if (ecdsa != null)
        {
            var ecParams = ecdsa.ExportParameters(false);

            // Determine curve name (crv) by coordinate length
            OIDCKeyCurveValues crv;
            int coordLen = ecParams.Q.X?.Length ?? 0;
            OIDCSigningAlg alg;
            if (coordLen == 32)
            {
                crv = OIDCKeyCurveValues.P256;
                alg = OIDCSigningAlg.ES256;
            }
            else if (coordLen == 48)
            {
                crv = OIDCKeyCurveValues.P384;
                alg = OIDCSigningAlg.ES384;
            }
            else if (coordLen == 66)
            {
                crv = OIDCKeyCurveValues.P521;
                alg = OIDCSigningAlg.ES512;
            }
            else
            {
                return null;
            }

            var ecKey = new OIDCJwksKeyResponse
            {
                kid = kid,
                kty = OIDCKeyType.EC,
                use = OIDCKeyUse.sig,
                alg = alg,
                crv = crv,
                x = Base64UrlEncoder.Encode(ecParams.Q.X),
                y = Base64UrlEncoder.Encode(ecParams.Q.Y),
                x5c = x5c,
                x5t = x5t,
                x5tS256 = x5tS256
            };

            return ecKey;
        }

        return null;
    }

    private OIDCJwksResponse CreateJwksResponse(ApplicationOption app)
    {
        X509Certificate2 cert = AddCertificateToCache(app);

        OIDCJwksKeyResponse? key = GetRSAKey(app, cert);

        return new(keys: key != null ? [key] : []);

    }

    public EtagContent HandleJwks(ApplicationOption app, CancellationToken ct)
    {
        var key = $"{app.ApplicationID}_JWKS";
        var result = _etagCache.Get(key);

        result ??= _etagCache.GetOrAdd(key, () =>
        {
            var jwks = CreateJwksResponse(app);
            return JsonSerializer.Serialize(jwks, _jsonUnsafeSerializationOptions);
        });

        return result;
    }

    public Task<string> HandleAuthorize(ApplicationOption app_config, string app_id, string user_id, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<bool> HandleEndSession(ApplicationOption app_config, string app_id, string user_id, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<string, object?>> HandlePAR(ApplicationOption app_config, string app_id, string token, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<string> HandleToken(ApplicationOption app_config, string app_id, string user_id, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<string, object?>> HandleUserInfo(ApplicationOption app_config, string app_id, string user_id, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

}
