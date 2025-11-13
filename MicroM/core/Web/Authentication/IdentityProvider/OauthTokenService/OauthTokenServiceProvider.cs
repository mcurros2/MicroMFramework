using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.Entities;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Data;

namespace MicroM.Web.Authentication.SSO;

public static class OauthTokenServiceProvider
{

    public static string EnsureSID(string? existing_sid = null)
    {
        if (!string.IsNullOrWhiteSpace(existing_sid))
        {
            return existing_sid;
        }
        return CryptClass.GenerateRandomBase64String(16);
    }

    public static ResultWithStatus<OauthCodeRequestRecord, ErrorResult> GetAuthCodeRequestRecord(IFormCollection form)
    {
        string? grant_type = form[WellknownIdentityConstants.GrantType];
        string? code = form[WellknownIdentityConstants.Code];
        string? redirect_uri = form[WellknownIdentityConstants.RedirectUri];
        string? code_verifier = form[WellknownIdentityConstants.CodeVerifier];
        string? client_id = form[WellknownIdentityConstants.ClientId];

        if (string.IsNullOrEmpty(grant_type) || !grant_type.Equals(WellknownIdentityConstants.AuthorizationCode, StringComparison.OrdinalIgnoreCase))
        {
            return new(null, new("invalid_request", "Missing or invalid 'grant_type' parameter. Only authorization_code is supported."));
        }

        if (string.IsNullOrEmpty(code_verifier))
        {
            return new(null, new("invalid_request", "Missing or invalid 'code_verifier' parameter."));
        }

        if (string.IsNullOrEmpty(code))
        {
            return new(null, new("invalid_request", "Missing or invalid 'code' parameter."));
        }

        if (string.IsNullOrEmpty(redirect_uri))
        {
            return new(null, new("invalid_request", "Missing or invalid 'redirect_uri' parameter."));
        }

        if (string.IsNullOrEmpty(client_id))
        {
            return new(null, new("invalid_request", "Missing or invalid 'client_id' parameter."));
        }

        return
            new(
                new
                (
                grant_type,
                code,
                redirect_uri,
                code_verifier,
                client_id
                ),
                null
            );
    }

    public static ResultWithStatus<OauthRefreshTokenRequestRecord, ErrorResult> GetRefreshTokenRequestRecord(IFormCollection form)
    {
        string? grant_type = form[WellknownIdentityConstants.GrantType];
        string? client_id = form[WellknownIdentityConstants.ClientId];
        string? refresh_token = form[WellknownIdentityConstants.RefreshToken];

        if (string.IsNullOrEmpty(grant_type) || !grant_type.Equals(WellknownIdentityConstants.RefreshToken, StringComparison.OrdinalIgnoreCase))
        {
            return new(null, new("invalid_request", "Missing or invalid 'grant_type' parameter. Only refresh_token is supported."));
        }

        if (string.IsNullOrEmpty(refresh_token))
        {
            return new(null, new("invalid_request", "Missing or invalid 'refresh_token' parameter."));
        }

        if (string.IsNullOrEmpty(client_id))
        {
            return new(null, new("invalid_request", "Missing or invalid 'client_id' parameter."));
        }

        return
            new(
                new
                (
                grant_type,
                refresh_token,
                client_id
                ),
                null
            );
    }

    public static (TokenResult? id_token, TokenResult? access_token, ErrorResult? error)
        GenerateAuthTokens(
            WebAPIJsonWebTokenHandler jwtHandler,
            ApplicationOption app,
            string sid,
            string? nonce,
            string client_id,
            string sub_hash
        )
    {
        if (string.IsNullOrWhiteSpace(sid))
        {
            return (null, null, new("invalid_request", "Missing or invalid 'sid' parameter."));
        }

        if (string.IsNullOrWhiteSpace(client_id))
        {
            return (null, null, new("invalid_request", "Missing or invalid 'client_id' parameter."));
        }

        if (string.IsNullOrWhiteSpace(sub_hash))
        {
            return (null, null, new("invalid_request", "Missing or invalid 'sub' parameter."));
        }

        var idClaims = new Dictionary<string, object>
        {
            [WellknownIdentityConstants.SubjectIdentifier] = sub_hash,
            [WellknownIdentityConstants.AuthorizedParty] = client_id,
            [WellknownIdentityConstants.SessionIdentifier] = sid
        };

        if (!string.IsNullOrWhiteSpace(nonce))
        {
            idClaims[WellknownIdentityConstants.Nonce] = nonce;
        }

        var accessClaims = new Dictionary<string, object>
        {
            [WellknownIdentityConstants.SubjectIdentifier] = sub_hash,
            [WellknownIdentityConstants.AuthorizedParty] = client_id,
            [WellknownIdentityConstants.Scope] = WellknownIdentityConstants.OpenID
        };

        // NEW: OIDC id_token is now generated via GenerateOidcIdToken (always signed; encrypted if certificate supports it).
        //       On unexpected failure, we fall back to legacy symmetric token generation to avoid breaking issuance.
        TokenResult? idTokenResult;
        try
        {
            idTokenResult = jwtHandler.GenerateOidcIdToken(idClaims, app, audience: client_id, nonce: nonce);
        }
        catch (Exception ex)
        {
            // NEW: Fallback path (graceful degradation) - log at warning level externally.
            idTokenResult = jwtHandler.GenerateJwtTokenWEBApi(idClaims, app, audience: client_id);
        }

        // Access token remains local API token (not OIDC; symmetric encryption/signing for server-side use).
        var accessTokenResult = jwtHandler.GenerateJwtTokenWEBApi(accessClaims, app);

        if (idTokenResult?.Token == null || accessTokenResult?.Token == null)
        {
            return (null, null, new("server_error", "Failed to generate tokens"));
        }

        return (idTokenResult, accessTokenResult, null);
    }

    public static async Task<(string refresh_token, DateTimeOffset refresh_expiration, string? sub, ErrorResult? error)> UpsertIdPSession(
        IEntityClient dbc,
        ILogger log,
        IDeviceIdService deviceIdService,
        IMicroMEncryption encryptor,
        ApplicationOption app,
        string client_id,
        string sid,
        string user_id,
        string sub_pepper,
        CancellationToken ct)
    {
        // IdP OIDC refresh token (subject-wide, not per client-device)
        var refreshToken = CryptClass.GenerateRandomBase64String(32);
        var refreshExpirationUtc = DateTimeOffset.UtcNow.AddHours(app.OIDCRefreshTokenExpirationHours);

        ErrorResult? error = null;

        var should_close = !(dbc.ConnectionState == ConnectionState.Open);
        string? sub = null;
        try
        {
            await dbc.Connect(CancellationToken.None);

            var sub_hash = await ApplicationOidcActiveSessions.CreateOrUpdateIdPSession(
                dbc,
                client_app_id: client_id,
                username: null,
                idp_user_id: user_id,
                device_id: WellknownIdentityConstants.Oidc, // Use a stable sentinel for device_id (token is backchannel; no browser device here)
                subject_pepper: sub_pepper,
                encryptor: encryptor,
                ct: ct,
                session_id: sid,
                idp_refresh_token: refreshToken,
                refresh_expiration_utc: refreshExpirationUtc.UtcDateTime!
            );

            sub = sub_hash;
        }
        catch (Exception ex)
        {
            // Do not fail token issuance on persistence errors
            log.LogError(ex, "IdP token: failed to persist client session (client_id={client}, user_id={user}, sid={sid})", client_id, user_id, sid);
            error = new("OAuth service error", ex.ToString());
        }
        finally
        {
            if (should_close) await dbc.Disconnect();
        }

        return (refreshToken, refreshExpirationUtc, sub, error);
    }

    public static (OauthCodeRequestRecord? code_request, OauthRefreshTokenRequestRecord? refresh_request, ErrorResult? error)
    ParseTokenRequest(IFormCollection form)
    {
        string? grant_type = form[WellknownIdentityConstants.GrantType];
        if (string.IsNullOrEmpty(grant_type))
        {
            return (null, null, new ErrorResult("invalid_request", "Missing 'grant_type' parameter."));
        }

        if (grant_type.Equals(WellknownIdentityConstants.AuthorizationCode, StringComparison.OrdinalIgnoreCase))
        {
            var (code_request, error) = GetAuthCodeRequestRecord(form);
            return (code_request, null, error);
        }
        else if (grant_type.Equals(WellknownIdentityConstants.RefreshToken, StringComparison.OrdinalIgnoreCase))
        {
            var (refresh_request, error) = GetRefreshTokenRequestRecord(form);
            return (null, refresh_request, error);
        }
        else
        {
            return (null, null, new ErrorResult("unsupported_grant_type", "Unsupported 'grant_type' parameter."));
        }
    }


}
