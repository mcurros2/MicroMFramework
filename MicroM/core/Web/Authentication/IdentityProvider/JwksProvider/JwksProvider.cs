using MicroM.Configuration;
using MicroM.Core;
using MicroM.Web.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace MicroM.Web.Authentication.SSO;

public record JWTProtectedHeaderResult
(
    bool IsJwe,
    string? Alg,
    string? Enc,
    string? Kid,
    string? ParseError
);

public static class JwksProvider
{
    // Allowed key mgmt algs
    private static readonly HashSet<string> AllowedTokenAlgs = new(StringComparer.Ordinal)
    {
        SecurityAlgorithms.RsaOAEP,

        // Ecdh not yet supported, we only use RSA certificates in IdP and Clients for now
        //SecurityAlgorithms.EcdhEsA256kw,
        //SecurityAlgorithms.EcdhEs
    };

    // Allowed content enc
    private static readonly HashSet<string> AllowedTokenEncs = new(StringComparer.Ordinal)
    {
        SecurityAlgorithms.Aes256Gcm,
        SecurityAlgorithms.Aes192Gcm,
        SecurityAlgorithms.Aes128Gcm
    };

    private static readonly HashSet<string> AllowedSigningAlgs = new(StringComparer.Ordinal)
    {
        SecurityAlgorithms.RsaSha256,
        SecurityAlgorithms.RsaSha384,
        SecurityAlgorithms.RsaSha512,
        SecurityAlgorithms.RsaSsaPssSha256,
        SecurityAlgorithms.RsaSsaPssSha384,
        SecurityAlgorithms.RsaSsaPssSha512
        // ECDSA not yet supported, we only use RSA certificates in IdP for now
    };

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
                // omitting use to allow both sig/enc usage
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

            switch (coordLen)
            {
                case 32:
                    crv = OIDCKeyCurveValues.P256;
                    break;
                case 48:
                    crv = OIDCKeyCurveValues.P384;
                    break;
                case 66:
                    crv = OIDCKeyCurveValues.P521;
                    break;
                default:
                    return null;
            }


            var ecKey = new OIDCJwksKeyResponse
            {
                kid = kid,
                kty = OIDCKeyType.EC,

                // omitting use to allow both sig/enc usage
                // omitting alg to allow ES* selection at runtime

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

    public static JWTProtectedHeaderResult TryReadProtectedHeader(string jwt)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jwt))
            {
                return new(false, null, null, null, "empty_token");
            }

            var parts = jwt.Split('.');
            // JWS: 3 parts; JWE: 5 parts. Any other length is invalid for our purposes.
            bool isJwe = parts.Length == 5;
            if (parts.Length != 3 && parts.Length != 5)
            {
                return new(false, null, null, null, "invalid_segment_count");
            }

            var headerB64 = parts[0];
            var headerJson = Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(headerB64));
            using var doc = JsonDocument.Parse(headerJson);
            var root = doc.RootElement;

            string? alg = root.ReadString(JwtHeaderParameterNames.Alg);
            string? enc = root.ReadString(JwtHeaderParameterNames.Enc);
            string? kid = root.ReadString(JwtHeaderParameterNames.Kid);

            return new(isJwe, alg, enc, kid, null);
        }
        catch (Exception ex)
        {
            return new(false, null, null, null, ex.Message);
        }
    }

    public static async Task<ResultWithStatus<JWTTokenResult, string>> ValidateIdTokenWithKeysAsync(
        IEnumerable<SecurityKey> signingKeys,
        string issuer,
        string audience,
        string idToken,
        X509Certificate2? clientDecryptionCertificate,
        JWTProtectedHeaderResult protectedHeader,
        CancellationToken ct)
    {
        try
        {
            var (isJwe, alg, enc, kid, parseError) = protectedHeader;
            if (isJwe)
            {
                if (parseError != null)
                {
                    return new(null, $"id_token header parse error: {parseError}");
                }
                if (string.IsNullOrWhiteSpace(alg) || !AllowedTokenAlgs.Contains(alg))
                {
                    return new(null, $"unsupported_encryption_alg: {alg ?? "<null>"}");
                }
                if (string.IsNullOrWhiteSpace(enc) || !AllowedTokenEncs.Contains(enc))
                {
                    return new(null, $"unsupported_encryption_enc: {enc ?? "<null>"}");
                }
            }
            else
            {
                // JWS (non-encrypted) → enforce allowed signing algs
                if (string.IsNullOrWhiteSpace(alg) || !AllowedSigningAlgs.Contains(alg))
                {
                    return new(null, $"unsupported_signing_alg: {alg}");
                }
            }

            var handler = new JsonWebTokenHandler();
            var parms = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                RequireSignedTokens = true,
                IssuerSigningKeys = signingKeys,
                ClockSkew = TimeSpan.FromMinutes(1),
                ValidAlgorithms = AllowedSigningAlgs
            };

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
