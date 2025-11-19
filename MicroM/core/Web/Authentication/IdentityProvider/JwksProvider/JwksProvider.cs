using MicroM.Configuration;
using MicroM.Core;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace MicroM.Web.Authentication.SSO;

public static class JwksProvider
{
    public static OIDCJwksKeyResponse? GetRSAKey(ApplicationOption app, X509Certificate2 cert)
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
            (
                kid: kid,
                kty: OIDCKeyType.RSA,
                use: OIDCKeyUse.sig,
                // omitting alg to allow RS*/PS* selection at runtime
                n: Base64UrlEncoder.Encode(par.Modulus),
                e: Base64UrlEncoder.Encode(par.Exponent),
                x5c: x5c,
                x5t: x5t,
                x5tS256: x5tS256
            );

            return rsaKey;
        }

        return null;
    }

    public static OIDCJwksKeyResponse? GetECDKey(ApplicationOption app, X509Certificate2 cert)
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

    public static async Task<JsonWebKeySet> FetchJwksAsync(IHttpClientFactory httpClientFactory, string jwksUri, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        if (string.IsNullOrWhiteSpace(jwksUri)) throw new ArgumentNullException(nameof(jwksUri));

        using var http = httpClientFactory.CreateClient(ConfigurationDefaults.HTTPClientJwksName);
        using var res = await http.GetAsync(jwksUri, ct);
        res.EnsureSuccessStatusCode();
        var jwksJson = await res.Content.ReadAsStringAsync(ct);
        return new JsonWebKeySet(jwksJson);
    }

    private static (bool isJwe, string? alg, string? enc, string? parseError) TryReadProtectedHeader(string jwt)
    {
        try
        {
            // JWS: 3 parts; JWE: 5 parts
            var parts = jwt.Split('.');
            if (parts.Length != 5)
                return (false, null, null, null);

            var headerB64 = parts[0];
            var headerJson = Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(headerB64));
            using var doc = JsonDocument.Parse(headerJson);
            var root = doc.RootElement;

            string? alg = root.TryGetProperty(JwtHeaderParameterNames.Alg, out var algEl) && algEl.ValueKind == JsonValueKind.String ? algEl.GetString() : null;
            string? enc = root.TryGetProperty(JwtHeaderParameterNames.Enc, out var encEl) && encEl.ValueKind == JsonValueKind.String ? encEl.GetString() : null;
            return (true, alg, enc, null);
        }
        catch (Exception ex)
        {
            return (true, null, null, ex.Message);
        }
    }

    public static async Task<ResultWithStatus<JWTTokenResult, string>> ValidateIdTokenAsync(
        IHttpClientFactory httpClientFactory,
        string jwksUri,
        string issuer,
        string audience,
        string idToken,
        CancellationToken ct)
    {
        return await ValidateIdTokenAsync(httpClientFactory, jwksUri, issuer, audience, idToken, clientDecryptionCertificate: null, ct);
    }

    public static async Task<ResultWithStatus<JWTTokenResult, string>> ValidateIdTokenAsync(
        IHttpClientFactory httpClientFactory,
        string jwksUri,
        string issuer,
        string audience,
        string idToken,
        X509Certificate2? clientDecryptionCertificate,
        CancellationToken ct)
    {
        try
        {
            // Enforce allowed JWE alg/enc when encrypted
            var (isJwe, alg, enc, parseError) = TryReadProtectedHeader(idToken);
            if (isJwe)
            {
                if (parseError != null)
                {
                    return new(null, $"id_token header parse error: {parseError}");
                }

                // Allowed key mgmt algs
                var allowedAlgs = new HashSet<string>(StringComparer.Ordinal)
                {
                    SecurityAlgorithms.RsaOAEP,
                    SecurityAlgorithms.EcdhEsA256kw,
                    SecurityAlgorithms.EcdhEs
                };
                // Allowed content enc
                var allowedEnc = new HashSet<string>(StringComparer.Ordinal)
                {
                    SecurityAlgorithms.Aes256Gcm
                };

                if (string.IsNullOrWhiteSpace(alg) || !allowedAlgs.Contains(alg))
                {
                    return new(null, $"unsupported_encryption_alg: {alg ?? "<null>"}");
                }
                if (string.IsNullOrWhiteSpace(enc) || !allowedEnc.Contains(enc))
                {
                    return new(null, $"unsupported_encryption_enc: {enc ?? "<null>"}");
                }
            }

            var jwks = await FetchJwksAsync(httpClientFactory, jwksUri, ct);

            var handler = new JsonWebTokenHandler();
            var parms = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                RequireSignedTokens = true,
                IssuerSigningKeys = jwks.Keys,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            // Enable JWE decryption when id_token is encrypted and the client has the private key
            if (clientDecryptionCertificate != null)
            {
                SecurityKey? decryptKey = null;

                if (clientDecryptionCertificate.GetRSAPrivateKey() != null)
                {
                    decryptKey = new X509SecurityKey(clientDecryptionCertificate);
                }
                else
                {
                    var ecdsa = clientDecryptionCertificate.GetECDsaPrivateKey();
                    if (ecdsa != null)
                    {
                        decryptKey = new ECDsaSecurityKey(ecdsa);
                    }
                }

                if (decryptKey != null)
                {
                    parms.TokenDecryptionKey = decryptKey;
                }
            }

            var result = await handler.ValidateTokenAsync(idToken, parms);
            if (!result.IsValid || result.SecurityToken is not JsonWebToken parsed)
                return new(null, $"Invalid id_token: {result.Exception?.Message}");

            var identity = result.ClaimsIdentity ?? new ClaimsIdentity(parsed.Claims.Select(c => new Claim(c.Type, c.Value)), WellknownIdentityConstants.Oidc);
            var principal = new ClaimsPrincipal(identity);

            DateTimeOffset? expUtc = null;
            var expClaim = identity.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
            if (expClaim != null && long.TryParse(expClaim, out var expSecs))
                expUtc = DateTimeOffset.FromUnixTimeSeconds(expSecs);

            return new(new(principal, parsed, expUtc), null);
        }
        catch (Exception ex)
        {
            return new(null, $"id_token validation error: {ex.Message}");
        }
    }
}
