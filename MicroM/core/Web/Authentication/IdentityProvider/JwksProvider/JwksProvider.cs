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
                if (string.IsNullOrWhiteSpace(alg) || !OIDCCryptoCapabilities.Client.AllowedIdTokenKeyManagementAlgs.Contains(alg))
                {
                    return new(null, $"unsupported_encryption_alg: {alg ?? "<null>"}");
                }
                if (string.IsNullOrWhiteSpace(enc) || !OIDCCryptoCapabilities.Client.AllowedIdTokenContentEncryptionAlgs.Contains(enc))
                {
                    return new(null, $"unsupported_encryption_enc: {enc ?? "<null>"}");
                }
            }
            else
            {
                // JWS (non-encrypted) → enforce allowed signing algs
                if (string.IsNullOrWhiteSpace(alg) || !OIDCCryptoCapabilities.Client.AllowedIdTokenSigningAlgorithms.Contains(alg))
                {
                    return new(null, $"unsupported_signing_alg: {alg ?? "<null>"}");
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
                // Hard anti-downgrade enforcement: do not accept algorithms outside our allow-list,
                // even if the external IdP advertises them in metadata.
                ValidAlgorithms = OIDCCryptoCapabilities.Client.AllowedIdTokenSigningAlgorithms
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


    /// <summary>
    /// Decrypts an OIDC request object (JWE) sent to our IdP and returns it as a JsonWebToken.
    /// If the request object is not encrypted (JWS only), it is just parsed and returned as-is.
    ///
    /// This method:
    /// - Enforces the same JWE "alg"/"enc" policy that we advertise in the IdP metadata
    ///   (request_object_encryption_alg_values_supported / request_object_encryption_enc_values_supported).
    /// - Uses the IdP certificate configured in ApplicationOption (OIDCCertificateBlob/Password)
    ///   as the decryption key.
    ///
    /// NOTE:
    /// - Signature validation of the request object against the client's JWKS is NOT done here, must be done later;
    ///   this method only handles decryption (plus basic header policy checks).
    /// </summary>
    public static async Task<ResultWithStatus<JsonWebToken, string>> DecryptRequestObjectAsync(
        ApplicationOption app,
        string requestObjectJwt,
        X509Certificate2 idp_cert,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(requestObjectJwt))
        {
            return new(null, "empty_request_object");
        }

        // Ensure we have an IdP certificate configured for decryption
        if (idp_cert == null)
        {
            return new(null, "IdP decryption certificate not configured");
        }

        // Parse protected header (works for both JWS and JWE)
        var header = TryReadProtectedHeader(requestObjectJwt);
        if (!string.IsNullOrEmpty(header.ParseError))
        {
            return new(null, $"request_object header parse error: {header.ParseError}");
        }

        var handler = new JsonWebTokenHandler();

        // If this is not a JWE, just parse and return the token as-is.
        if (!header.IsJwe)
        {
            try
            {
                var jwt = handler.ReadJsonWebToken(requestObjectJwt);
                return new(jwt, null);
            }
            catch (Exception ex)
            {
                return new(null, $"request_object parse error: {ex.Message}");
            }
        }

        // --- JWE path: request object is encrypted TO our IdP ---

        // Enforce JWE "alg" and "enc" policy consistent with the IdP metadata.
        var allowedAlgs = OIDCCryptoCapabilities.Idp.AllowedRequestObjectKeyManagementAlgStrings;
        var allowedEncs = OIDCCryptoCapabilities.Idp.AllowedRequestObjectContentEncryptionAlgStrings;

        if (string.IsNullOrWhiteSpace(header.Alg) || !allowedAlgs.Contains(header.Alg))
        {
            return new(null, $"unsupported_request_object_encryption_alg: {header.Alg ?? "<null>"}");
        }

        if (string.IsNullOrWhiteSpace(header.Enc) || !allowedEncs.Contains(header.Enc))
        {
            return new(null, $"unsupported_request_object_encryption_enc: {header.Enc ?? "<null>"}");
        }

        try
        {
            var decryptionKey = new X509SecurityKey(idp_cert);

            var parms = new TokenValidationParameters
            {
                // We only care about decryption here, not about issuer/audience/lifetime.
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = false,
                RequireSignedTokens = false,

                TokenDecryptionKey = decryptionKey,
            };

            var result = await handler.ValidateTokenAsync(requestObjectJwt, parms);

            if (!result.IsValid || result.SecurityToken is not JsonWebToken decryptedJwt)
            {
                return new(null, $"request_object decrypt error: {result.Exception?.Message ?? "unknown error"}");
            }

            return new(decryptedJwt, null);
        }
        catch (Exception ex)
        {
            return new(null, $"request_object decrypt error: {ex.Message}");
        }
    }

    public static async Task<ResultWithStatus<JsonWebToken, string>> ValidateSignedRequestObjectAsync(
    string signedRequestObjectJwt,
    IEnumerable<SecurityKey> clientSigningKeys,
    CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(signedRequestObjectJwt))
        {
            return new(null, "empty_request_object");
        }

        if (clientSigningKeys == null || !clientSigningKeys.Any())
        {
            return new(null, "no_client_signing_keys");
        }

        // Basic header parsing to enforce alg allow-list
        var header = TryReadProtectedHeader(signedRequestObjectJwt);
        if (!string.IsNullOrEmpty(header.ParseError))
        {
            return new(null, $"request_object header parse error: {header.ParseError}");
        }

        if (header.IsJwe)
        {
            // This helper is meant for a *signed* JWT (JWS), not an encrypted JWE.
            // Encryption should have been handled by DecryptRequestObjectAsync first.
            return new(null, "request_object_is_encrypted_use_decrypt_first");
        }

        var alg = header.Alg;
        if (string.IsNullOrWhiteSpace(alg))
        {
            return new(null, "request_object_missing_alg");
        }

        // Enforce IdP’s allow-list for client assertions / request objects
        var allowedAlgs = OIDCCryptoCapabilities.Idp.AllowedClientAssertionSigningAlgStrings;

        if (!allowedAlgs.Contains(alg))
        {
            return new(null, $"unsupported_request_object_signing_alg: {alg}");
        }

        var handler = new JsonWebTokenHandler();
        var parms = new TokenValidationParameters
        {
            // Request Object validation is closer to client_assertion than to id_token:
            // - iss must be the client_id
            // - aud must be the authorization endpoint or issuer (you can tighten later)
            ValidateIssuer = false,   // can be tightened later to client_id
            ValidateAudience = false, // can be tightened later to authorization endpoint
            ValidateLifetime = true,  // exp/nbf on request objects SHOULD be enforced
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = clientSigningKeys,
            // Hard anti-downgrade: only accept our allow-listed algorithms
            ValidAlgorithms = allowedAlgs
        };

        var result = await handler.ValidateTokenAsync(signedRequestObjectJwt, parms);

        if (!result.IsValid || result.SecurityToken is not JsonWebToken jwt)
        {
            return new(null, $"request_object signature validation failed: {result.Exception?.Message}");
        }

        return new(jwt, null);
    }

}
