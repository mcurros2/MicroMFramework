using MicroM.Configuration;
using MicroM.Core;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.Extensions;
using MicroM.Web.Extensions;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace MicroM.Web.Authentication.SSO;

public sealed record EffectiveIdTokenAlgs(
    ISet<string> AllowedSigningAlgs,
    ISet<string> AllowedKeyMgmtAlgs,
    ISet<string> AllowedEncAlgs
    );

public static class OIDCClientServiceProvider
{
    public static ResultWithStatus<OIDCTokenResponse?, string?> ParseTokenResponse(string tokenResponseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(tokenResponseBody);

            var response = new OIDCTokenResponse
            (
                token_type: doc.RootElement.ReadString(WellknownIdentityConstants.TokenType) ?? "",
                expires_in: doc.RootElement.ReadInt32(WellknownIdentityConstants.ExpiresIn) ?? 0,
                access_token: doc.RootElement.ReadString(WellknownIdentityConstants.AccessToken) ?? "",
                refresh_token: doc.RootElement.ReadString(WellknownIdentityConstants.RefreshToken),
                id_token: doc.RootElement.ReadString(WellknownIdentityConstants.IdToken),
                scope: doc.RootElement.ReadString(WellknownIdentityConstants.Scope) ?? "",
                refresh_expiration_utc: doc.RootElement.ReadString(WellknownIdentityConstants.RefreshExpirationUtc)
            );

            return new(response, null);
        }
        catch (Exception ex)
        {
            return new(null, $"Failed to parse token response {ex}");
        }
    }

    public static async Task<ResultWithStatus<OIDCTokenResponse?, string?>> PostToTokenEndpoint(
        IOIDCHttpClient oidcHttpClient,
        string tokenEndpoint,
        Dictionary<string, string> tokenForm,
        CancellationToken ct
        )
    {
        try
        {
            var tokenResult = await oidcHttpClient.PostTokenAsync(tokenEndpoint, tokenForm, authorization: null, ct);
            if (!tokenResult.IsSuccessStatusCode)
            {
                return new(null, $"Token exchange failed: {tokenResult.Body}. Error: {tokenResult.Error}");
            }

            return ParseTokenResponse(tokenResult.Body);
        }
        catch (Exception ex)
        {
            return new(null, $"Token request error: {ex.Message}");
        }
    }

    public async static Task<ResultWithStatus<JWTTokenResult?, string?>> ValidateToken(
        IApplicationCertificateCacheService certificate_cache,
        ApplicationOption app,
        OIDCWellKnownResponse wellknown,
        string id_token,
        IOIDCHttpClient oidcHttpClient,
        IEtagCacheService<OIDCJwksResponse> remote_jwks_cache,
        CancellationToken ct
        )
    {
        string issuer = wellknown.issuer;
        string jwksUri = wellknown.jwks_uri;
        string clientId = app.ApplicationID;

        var clientDecryptCert = certificate_cache.GetCertificate(app);

        var jwksResult = await JwksProvider.FetchAndCacheRemoteJwksAsync(jwksUri, oidcHttpClient, remote_jwks_cache, ct);

        var header = JwksProvider.TryReadProtectedHeader(id_token);

        var signingKeys = jwksResult.Keys.Values;

        if (jwksResult.Keys.Count > 0 && header.Kid != null && !jwksResult.Keys.ContainsKey(header.Kid))
        {
            return new(null, $"JWKS_HEADER_KID_NOT_FOUND - Client_id: {clientId} - refetching Jwks {jwksUri}");
        }

        var effectiveAlgs = BuildEffectiveIdTokenAlgs(wellknown);

        var result = await JwksProvider.ValidateIdTokenWithKeysAsync(
            signingKeys, issuer, clientId, id_token, clientDecryptCert, header,
            effectiveAlgs.AllowedSigningAlgs, effectiveAlgs.AllowedKeyMgmtAlgs, effectiveAlgs.AllowedEncAlgs,
            ct);

        if (result == null)
        {
            return new(null, "Token validation returned null result");
        }

        return new(result.Result, result.Status);
    }

    public static (Dictionary<string, string>? valid_form, (string? error, string? error_description)?) ValidateClientSignInForm(ApplicationOption client_app, IFormCollection form)
    {

        if (client_app.IdentityProviderRoleType != nameof(IdentityProviderRole.IDPClient))
            return (null, (error: "invalid_app", error_description: "Application is not configured as an Identity Provider Client"));

        var forward = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var k in form.Keys)
        {
            forward[k] = form[k].ToString();
        }

        if (!forward.TryGetValue(WellknownIdentityConstants.ResponseType, out var response_type) || response_type != WellknownIdentityConstants.Code)
            return (null, (error: "invalid_request", error_description: "response_type must be 'code'"));

        if (!forward.TryGetValue(WellknownIdentityConstants.ClientId, out var clientId) || string.IsNullOrWhiteSpace(clientId) || clientId != client_app.ApplicationID)
            return (null, (error: "invalid_request", error_description: "Invalid client_id"));

        if (!forward.TryGetValue(WellknownIdentityConstants.RedirectUri, out var redirectUri) || string.IsNullOrWhiteSpace(redirectUri))
            return (null, (error: "invalid_request", error_description: "redirect_uri is required"));

        if (!forward.TryGetValue(WellknownIdentityConstants.Scope, out var scope) || string.IsNullOrWhiteSpace(scope))
            return (null, (error: "invalid_request", error_description: "scope is required"));

        if (!forward.TryGetValue(WellknownIdentityConstants.CodeChallenge, out var codeChallenge) || string.IsNullOrWhiteSpace(codeChallenge))
            return (null, (error: "invalid_request", error_description: "code_challenge is required"));

        if (!forward.TryGetValue(WellknownIdentityConstants.CodeChallengeMethod, out var codeChallengeMethod) || string.IsNullOrWhiteSpace(codeChallengeMethod))
            return (null, (error: "invalid_request", error_description: "code_challenge_method is required"));

        forward.TryGetValue(WellknownIdentityConstants.State, out var state);
        if (string.IsNullOrEmpty(state))
            return (null, (error: "invalid_request", error_description: "state is required"));

        forward.TryGetValue(WellknownIdentityConstants.Nonce, out var nonce);
        if (string.IsNullOrEmpty(nonce))
            return (null, (error: "invalid_request", error_description: "nonce is required"));

        return (forward, null);
    }

    /// <summary>
    /// Validates whether the specified target link URI is allowed for the given application configured as IDPClient and returns a normalized URI
    /// or an error code. Returns only path and query.
    /// </summary>
    public static (string? Normalized, (string? error, string? error_description)?) ValidateTargetLinkURIAllowed(string? uri, ApplicationOption app)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            return (null, null);
        }

        var value = uri.Trim();

        if (value.Length > 2048)
        {
            return (null, ("invalid_target_link_uri_length", null));
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var targetUri))
        {
            return (null, ("invalid_target_link_uri_format", value.Truncate(2048)));
        }

        bool isAllowed = false;
        foreach (var allowedUrl in app.FrontendURLS)
        {
            if (Uri.TryCreate(allowedUrl, UriKind.Absolute, out var allowedUri))
            {
                if (targetUri.Scheme.Equals(allowedUri.Scheme, StringComparison.OrdinalIgnoreCase) &&
                    targetUri.Host.Equals(allowedUri.Host, StringComparison.OrdinalIgnoreCase) &&
                    targetUri.Port == allowedUri.Port)
                {
                    isAllowed = true;
                    break;
                }
            }
            else
            {
                return (null, ("invalid_configured_frontend_url", allowedUrl));
            }
        }

        if (!isAllowed)
        {
            return (null, ("invalid_target_link_uri_not_allowed", null));
        }

        return (targetUri.PathAndQuery, null);
    }

    public static EffectiveIdTokenAlgs BuildEffectiveIdTokenAlgs(OIDCWellKnownResponse wk)
    {
        // 1) SIGNING
        var signing = new HashSet<string>();

        if (wk.id_token_signing_alg_values_supported != null &&
            wk.id_token_signing_alg_values_supported.Count > 0)
        {
            foreach (var jwtAlg in wk.id_token_signing_alg_values_supported)
            {
                string currentAlg = jwtAlg.ToString();
                if (OIDCCryptoCapabilities.Client.AllowedIdTokenSigningAlgorithms.Contains(currentAlg))
                {
                    signing.Add(currentAlg);
                }
            }
        }

        // 2) KEY MGMT (alg)
        var km = new HashSet<string>();

        if (wk.id_token_encryption_alg_values_supported != null &&
            wk.id_token_encryption_alg_values_supported.Count > 0)
        {
            foreach (var jwtAlg in wk.id_token_encryption_alg_values_supported)
            {
                string currentAlg = jwtAlg.ToAlgString();
                if (OIDCCryptoCapabilities.Client.AllowedIdTokenKeyManagementAlgs.Contains(currentAlg))
                {
                    km.Add(currentAlg);
                }
            }
        }

        // 3) ENC (enc)
        var enc = new HashSet<string>();

        if (wk.id_token_encryption_enc_values_supported != null &&
            wk.id_token_encryption_enc_values_supported.Count > 0)
        {
            foreach (var jwtEnc in wk.id_token_encryption_enc_values_supported)
            {
                string currentAlg = jwtEnc.ToAlgString();
                if (OIDCCryptoCapabilities.Client.AllowedIdTokenContentEncryptionAlgs.Contains(currentAlg))
                {
                    enc.Add(currentAlg);
                }
            }
        }

        return new(signing, km, enc);
    }


}
