using MicroM.Configuration;
using MicroM.Core;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.Extensions;
using MicroM.Web.Authentication.SSO;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

namespace MicroM.Web.Authentication;

public record TokenResult { public string? Token { get; init; } public SecurityTokenDescriptor? SD { get; init; } }

public class WebAPIJsonWebTokenHandler(
    IMicroMAppConfiguration app_config,
    IHttpContextAccessor http_context,
    IApplicationCertificateCacheService cert_cache,
    IJWKSFetchCacheService jwks_cache,
    ILogger<WebAPIJsonWebTokenHandler> logger
    ) : JsonWebTokenHandler
{
    private static TokenValidationParameters GetValidationParameters(ApplicationOption app)
    {

        TokenValidationParameters parms = new();

        var security_key = CryptClass.GetSecurityKey(app.JWTKey, app.ApplicationName);
        parms.ValidIssuer = app.JWTIssuer;
        parms.ValidAudience = string.IsNullOrEmpty(app.JWTAudience) ? app.JWTIssuer : app.JWTAudience;
        parms.IssuerSigningKey = security_key;
        parms.TokenDecryptionKey = security_key;
        parms.ClockSkew = TimeSpan.Zero;

        return parms;
    }

    /// <summary>
    /// Generate a JwtToken with encryption. Claims will be encrypted and cannot be used in the client. 
    /// Claims here are mean to be used from the backend and protected from tampering within the client.
    /// </summary>
    public TokenResult GenerateJwtTokenWEBApi(Dictionary<string, object> claims, ApplicationOption app, string? audience = null)
    {
        var securityKey = CryptClass.GetSecurityKey(app.JWTKey, app.ApplicationName);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var encrypting_credentials = new EncryptingCredentials(securityKey, SecurityAlgorithms.Aes256KeyWrap, SecurityAlgorithms.Aes256CbcHmacSha512);

        // check claims for null or DBNull values and replace with empty string
        foreach (var claim in claims)
        {
            if (claim.Value == null || claim.Value == DBNull.Value)
            {
                claims[claim.Key] = "";
            }
        }

        var sd = new SecurityTokenDescriptor()
        {
            Issuer = app.JWTIssuer,
            Expires = DateTime.UtcNow.AddMinutes(app.JWTTokenExpirationMinutes),
            SigningCredentials = credentials,
            EncryptingCredentials = encrypting_credentials,
            Claims = claims,
            Audience = !audience.IsNullOrEmpty() ? audience : (string.IsNullOrEmpty(app.JWTAudience) ? app.JWTIssuer : app.JWTAudience)
        };

        string token = CreateToken(sd);

        return new() { Token = token, SD = sd };
    }

    public async Task<ClaimsPrincipal?> ValidateExpiredToken(ApplicationOption app, string token)
    {
        var parms = GetValidationParameters(app);
        parms.ValidateAudience = false;
        parms.ValidateIssuer = false;
        parms.ValidateIssuerSigningKey = true;
        parms.ValidateLifetime = false;
        parms.ValidateActor = false;

        ClaimsPrincipal? principal = null;
        // MMC: When using SQL Server Authenticator, a new encryption key is generated every time the server is restarted
        // this validator uses in-memory data to keep configuration and is not persisted on purpose.
        // So we expect that when refreshing to get a SecurityTokenKeyWrapException
        try
        {
            // MMC: we have overrided the validate token to get dynamic parameters based on app_id, so we need to call base with the new parameters
            var validationResult = await base.ValidateTokenAsync(token, parms);

            if (validationResult.IsValid)
            {
                principal = new ClaimsPrincipal(validationResult.ClaimsIdentity);
            }
            else
            {
                logger.LogWarning("ValidateExpiredToken: Failed validation: {message}", validationResult.Exception.Message);
            }

            if (validationResult.SecurityToken is not JsonWebToken) throw new SecurityTokenException("Invalid token: not a JsonWebToken");

            return principal;

        }
        catch (Exception ex)
        {
            if (app.AuthenticationType == nameof(AuthenticationTypes.SQLServerAuthentication))
            {
                logger.LogWarning("ValidateExpiredToken: Invalid encryption key for app {app_id} with SQLServerAuthentication (this is expected with this type of auth). {message}", app.ApplicationID, ex.Message);
            }
            else
            {
                logger.LogError("ValidateExpiredToken error: {ex}", ex);
            }
        }

        return null;
    }


    private TokenValidationParameters GetValidationParametersFromContext()
    {
        string app_id = http_context.HttpContext?.Request?.RouteValues["app_id"]?.ToString() ?? "";
        TokenValidationParameters token_parms = new();
        if (!string.IsNullOrEmpty(app_id))
        {
            ApplicationOption? app = app_config.GetAppConfiguration(app_id);
            if (app != null) token_parms = GetValidationParameters(app);
        }

        return token_parms;
    }

    private void SetContextTokenValidationParameters(TokenValidationParameters target_parms)
    {
        TokenValidationParameters context_parms = GetValidationParametersFromContext();

        // MMC: merge with received validation parameters
        if (target_parms != null)
        {
            target_parms.ValidIssuer = context_parms.ValidIssuer;
            target_parms.ValidAudience = context_parms.ValidAudience;
            target_parms.IssuerSigningKey = context_parms.IssuerSigningKey;
            target_parms.TokenDecryptionKey = context_parms.TokenDecryptionKey;
            target_parms.ClockSkew = context_parms.ClockSkew;
        }

    }

    public override Task<TokenValidationResult> ValidateTokenAsync(string token, TokenValidationParameters validationParameters)
    {

        SetContextTokenValidationParameters(validationParameters);

        return base.ValidateTokenAsync(token, validationParameters);
    }

    public override Task<TokenValidationResult> ValidateTokenAsync(SecurityToken token, TokenValidationParameters validationParameters)
    {
        SetContextTokenValidationParameters(validationParameters);

        return base.ValidateTokenAsync(token, validationParameters);
    }

    // OIDC IdP support (signing always; encryption when certificate allows)
    private static SigningCredentials? GetOidcSigningCredentials(ApplicationOption app, X509Certificate2 cert)
    {
        // Prefer configured signing alg when available; fallback by key type
        var preferred = app.OIDCTokenSigningAlg;

        if (cert.GetRSAPrivateKey() != null)
        {
            var key = new X509SecurityKey(cert);
            return preferred switch
            {
                OIDCSigningAlg.PS512 => new SigningCredentials(key, SecurityAlgorithms.RsaSsaPssSha512),
                OIDCSigningAlg.PS384 => new SigningCredentials(key, SecurityAlgorithms.RsaSsaPssSha384),
                OIDCSigningAlg.PS256 => new SigningCredentials(key, SecurityAlgorithms.RsaSsaPssSha256),
                OIDCSigningAlg.RS512 => new SigningCredentials(key, SecurityAlgorithms.RsaSha512),
                OIDCSigningAlg.RS384 => new SigningCredentials(key, SecurityAlgorithms.RsaSha384),
                OIDCSigningAlg.RS256 => new SigningCredentials(key, SecurityAlgorithms.RsaSha256),
                // DEFAULT: RS256
                _ => new SigningCredentials(key, SecurityAlgorithms.RsaSha256)
            };
        }

        if (cert.GetECDsaPrivateKey() != null)
        {
            var key = new X509SecurityKey(cert);
            return preferred switch
            {
                OIDCSigningAlg.ES512 => new SigningCredentials(key, SecurityAlgorithms.EcdsaSha512),
                OIDCSigningAlg.ES384 => new SigningCredentials(key, SecurityAlgorithms.EcdsaSha384),
                OIDCSigningAlg.ES256 => new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256),
                // DEFAULT: ES256
                _ => new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256)
            };
        }

        return null;
    }

    // Build EncryptingCredentials for the audience (client) based on local client certificate or remote client JWKS
    private async Task<EncryptingCredentials?> ResolveEncryptingCredentialsForAudience(ApplicationOption idpApp, string audience)
    {
        try
        {
            // Must have client registration to resolve its keying material
            if (idpApp.OIDCClientConfiguration == null ||
                !idpApp.OIDCClientConfiguration.TryGetValue(audience, out var clientCfg))
            {
                return null;
            }

            // 1) Local client certificate path
            if (!string.IsNullOrWhiteSpace(clientCfg.CertificateUniqueID))
            {
                var clientApp = app_config.GetAppConfiguration(audience);
                if (clientApp != null)
                {
                    var clientCert = cert_cache.GetCertificate(clientApp);
                    if (clientCert != null)
                    {
                        // Prefer RSA-OAEP for RSA; ECDH-ES(+A256KW) for EC
                        return GetOidcEncryptingCredentials(clientCert);
                    }
                }
            }

            // 2) Remote client JWKS path
            if (!string.IsNullOrWhiteSpace(clientCfg.URLClientJWKS))
            {
                var jwks = await jwks_cache.GetAsync(clientCfg.URLClientJWKS, CancellationToken.None);

                // Prefer RSA first
                foreach (var key in jwks.Keys.Values)
                {
                    try
                    {
                        if (key is JsonWebKey jwkRsa && string.Equals(jwkRsa.Kty, "RSA", StringComparison.OrdinalIgnoreCase))
                        {
                            return new EncryptingCredentials(jwkRsa, SecurityAlgorithms.RsaOAEP, SecurityAlgorithms.Aes256Gcm);
                        }
                        if (key is RsaSecurityKey rsaKey)
                        {
                            return new EncryptingCredentials(rsaKey, SecurityAlgorithms.RsaOAEP, SecurityAlgorithms.Aes256Gcm);
                        }
                        if (key is X509SecurityKey x509Key && x509Key.PublicKey is not null && x509Key.PublicKey.GetType().Name.Contains("RSA", StringComparison.OrdinalIgnoreCase))
                        {
                            return new EncryptingCredentials(x509Key, SecurityAlgorithms.RsaOAEP, SecurityAlgorithms.Aes256Gcm);
                        }
                    }
                    catch { /* try next key */ }
                }

                // Then try EC (ECDH-ES + A256KW preferred)
                foreach (var key in jwks.Keys.Values)
                {
                    try
                    {
                        if (key is JsonWebKey jwkEc && string.Equals(jwkEc.Kty, "EC", StringComparison.OrdinalIgnoreCase))
                        {
                            // Try ECDH-ES with key wrapping first
                            try { return new EncryptingCredentials(jwkEc, SecurityAlgorithms.EcdhEsA256kw, SecurityAlgorithms.Aes256Gcm); } catch { }
                            try { return new EncryptingCredentials(jwkEc, SecurityAlgorithms.EcdhEs, SecurityAlgorithms.Aes256Gcm); } catch { }
                        }
                        if (key is ECDsaSecurityKey ecKey)
                        {
                            try { return new EncryptingCredentials(ecKey, SecurityAlgorithms.EcdhEsA256kw, SecurityAlgorithms.Aes256Gcm); } catch { }
                            try { return new EncryptingCredentials(ecKey, SecurityAlgorithms.EcdhEs, SecurityAlgorithms.Aes256Gcm); } catch { }
                        }
                    }
                    catch { /* try next key */ }
                }
            }
        }
        catch
        {
            // swallow and fall back to signed-only
        }

        return null;
    }

    // Select asymmetric JWE based on certificate capabilities (RSA or EC). Client ultimately decides via advertised metadata.
    private static EncryptingCredentials? GetOidcEncryptingCredentials(X509Certificate2 cert)
    {
        // NOTE: RSA_OAEP_256 removed (unsupported). Ordered preference now:
        // RSA: RSA_OAEP > RSA1_5
        // EC:  ECDH_ES_A256KW > ECDH_ES (direct)
        // Content encryption preference: A256GCM > A256CBC-HS512

        string[] candidateKeyAlgs;
        if (cert.GetRSAPublicKey() != null)
        {
            candidateKeyAlgs =
            [
                SecurityAlgorithms.RsaOAEP,
                SecurityAlgorithms.RsaPKCS1
            ];
        }
        else if (cert.GetECDsaPublicKey() != null)
        {
            candidateKeyAlgs =
            [
                SecurityAlgorithms.EcdhEsA256kw,
                SecurityAlgorithms.EcdhEs
            ];
        }
        else
        {
            return null;
        }

        string[] contentAlgs =
        [
            SecurityAlgorithms.Aes256Gcm,
            SecurityAlgorithms.Aes256CbcHmacSha512
        ];

        SecurityKey? keySecurityKey = null;
        if (cert.GetRSAPublicKey() != null)
        {
            keySecurityKey = new X509SecurityKey(cert);
        }
        else
        {
            var ecdsa = cert.GetECDsaPublicKey();
            if (ecdsa != null)
            {
                keySecurityKey = new ECDsaSecurityKey(ecdsa);
            }
        }

        if (keySecurityKey == null) return null;

        foreach (var keyAlg in candidateKeyAlgs)
        {
            foreach (var encAlg in contentAlgs)
            {
                try
                {
                    return new EncryptingCredentials(keySecurityKey, keyAlg, encAlg);
                }
                catch
                {
                }
            }
        }

        return null;
    }

    // Creates a signed id_token; if encryption to the audience is possible, returns JWE (sign-then-encrypt).
    public async Task<TokenResult> GenerateOidcIdToken(Dictionary<string, object> claims, ApplicationOption app, string audience, string? nonce = null)
    {
        if (nonce != null && !claims.ContainsKey(WellknownIdentityConstants.Nonce))
            claims[WellknownIdentityConstants.Nonce] = nonce;

        foreach (var kv in claims.ToArray())
        {
            if (kv.Value == null || kv.Value == DBNull.Value)
                claims[kv.Key] = "";
        }

        if (app.OIDCCertificateBlob == null || app.OIDCCertificateBlob.Length == 0)
            throw new InvalidOperationException("OIDC certificate blob not configured for id_token.");

        using var idpCert = new X509Certificate2(app.OIDCCertificateBlob, app.OIDCCertificatePassword, X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);

        var signing = GetOidcSigningCredentials(app, idpCert) ?? throw new InvalidOperationException("Unable to resolve signing credentials for id_token.");

        // NEW: resolve encrypting credentials based on the audience (client)
        var encrypting = await ResolveEncryptingCredentialsForAudience(app, audience);

        var sd = new SecurityTokenDescriptor
        {
            Issuer = app.JWTIssuer,
            Audience = audience,
            Expires = DateTime.UtcNow.AddMinutes(app.JWTTokenExpirationMinutes),
            SigningCredentials = signing,
            EncryptingCredentials = encrypting,
            Claims = claims
        };

        var token = CreateToken(sd);
        return new TokenResult { Token = token, SD = sd };
    }
}
