using MicroM.Configuration;
using MicroM.Core;
using MicroM.DataDictionary.Entities;
using MicroM.Extensions;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MicroM.Web.Authentication.SSO;

public class OauthTokenService(
    IAuthorizationCodeService codeService,
    WebAPIJsonWebTokenHandler jwtHandler,
    IDeviceIdService deviceIdService,
    IMicroMEncryption encryptor,
    ILogger<OauthTokenService> log) : IOauthTokenService
{
    private static string SidMarker(string? sid) => string.IsNullOrWhiteSpace(sid) ? "none" : sid.Truncate(16);

    private async Task<ResultWithStatus<OIDCTokenResponse, ErrorResult>> HandleRefreshTokenGrant(ApplicationOption app, IFormCollection form, string authenticated_client_app, CancellationToken ct)
    {
        var (record, error) = OauthTokenServiceProvider.GetRefreshTokenRequestRecord(form);

        if (record == null || error != null)
        {
            log.LogWarning("OIDC_TOKEN_REFRESH_INVALID client_id={clientId} error={error}", record?.client_id ?? "n/a", error?.Error ?? "parse_error");
            return new(null, error);
        }

        if (!string.Equals(record.client_id, authenticated_client_app, StringComparison.OrdinalIgnoreCase))
        {
            log.LogWarning("OIDC_TOKEN_REFRESH_CLIENT_MISMATCH form_client_id={formClient} auth_client_id={authClient}", record.client_id, authenticated_client_app);
            return new(null, new("invalid_client", "client_id mismatch with authenticated client"));
        }

        var registeredClients = app.OIDCClientConfiguration;
        if (registeredClients == null || !registeredClients.TryGetValue(record.client_id, out var clientCfg))
        {
            log.LogWarning("OIDC_TOKEN_REFRESH_UNKNOWN_CLIENT client_id={clientId}", record.client_id);
            return new(null, new("invalid_client", "Unknown client_id"));
        }

        var sub_pepper = clientCfg.OIDCSubjectPepper;
        if (string.IsNullOrEmpty(sub_pepper))
        {
            log.LogWarning("OIDC_TOKEN_REFRESH_CLIENT_MISCONFIG client_id={clientId} missing=sub_pepper", record.client_id);
            return new(null, new("client_not_configured", "Missing SUBP"));
        }

        using var dbc = app.CreateDatabaseClient(log, deviceIdService, null);
        try
        {
            await dbc.Connect(ct);

            var session = await ApplicationOidcActiveSessions.GetSessionByRefreshToken(
                ec: dbc,
                client_id: record.client_id,
                refresh_token: record.refresh_token,
                ct: ct,
                encryptor: encryptor);

            if (session == null)
            {
                log.LogWarning("OIDC_TOKEN_REFRESH_INVALID_GRANT client_id={clientId} reason=refresh_not_found", record.client_id);
                return new(null, new("invalid_grant", "Invalid refresh token"));
            }

            if (session.dt_refresh_expiration == null || session.dt_refresh_expiration <= DateTime.UtcNow)
            {
                log.LogWarning("OIDC_TOKEN_REFRESH_EXPIRED client_id={clientId} sid={sid} refresh_expired_at={expiredAt:o}", record.client_id, SidMarker(session.vc_oidc_session_id), session.dt_refresh_expiration);
                return new(null, new("invalid_grant", "Refresh token expired"));
            }

            var (refresh_token, refresh_expiration, sub, upsert_error) = await OauthTokenServiceProvider.UpsertIdPSession
            (
                dbc,
                log,
                deviceIdService,
                encryptor,
                app,
                record.client_id,
                session.vc_oidc_session_id,
                session.c_user_id,
                sub_pepper,
                ct
            );

            var (idTokenResult, accessTokenResult, tokenError) =
                await OauthTokenServiceProvider.GenerateAuthTokens(jwtHandler, app, session.vc_oidc_session_id, null, record.client_id, sub!);

            if (idTokenResult?.Token == null || accessTokenResult?.Token == null || tokenError != null || refresh_token == null)
            {
                log.LogError("OIDC_TOKEN_REFRESH_GENERATE_FAILED client_id={clientId} sid={sid} error={error}", record.client_id, SidMarker(session.vc_oidc_session_id), tokenError?.Error ?? "token_generation_failed");
                return new(null, new("server_error", "Failed to generate tokens"));
            }

            var token_response = new OIDCTokenResponse(
                token_type: WellknownIdentityConstants.Bearer,
                expires_in: app.JWTTokenExpirationMinutes * 60,
                access_token: accessTokenResult.Token,
                refresh_token: refresh_token,
                id_token: idTokenResult.Token,
                scope: WellknownIdentityConstants.OpenID,
                refresh_expiration_utc: refresh_expiration.ToString("o")
            );

            log.LogInformation("OIDC_TOKEN_REFRESH_ISSUED client_id={clientId} sid={sid} refresh_expiration={refreshExp:o} user_id={userId} id_token_len={idLen} access_token_len={accLen} refresh_token_len={refLen} upsert_error={upsertErr}",
                record.client_id,
                SidMarker(session.vc_oidc_session_id),
                refresh_expiration,
                session.c_user_id,
                idTokenResult.Token.Length,
                accessTokenResult.Token.Length,
                refresh_token.Length,
                upsert_error?.Error ?? "none");

            return new(token_response, null);
        }
        finally
        {
            await dbc.Disconnect();
        }
    }

    private async Task<ResultWithStatus<OIDCTokenResponse, ErrorResult>> HandleAuthTokenRequest(ApplicationOption app, IFormCollection form, string authenticated_client_app, CancellationToken ct)
    {
        var (request_record, error) = OauthTokenServiceProvider.GetAuthCodeRequestRecord(form);

        if (error != null || request_record == null)
        {
            log.LogWarning("OIDC_TOKEN_CODE_PARSE_FAILED client_id={clientId} error={error}", request_record?.client_id ?? "n/a", error?.Error ?? "parse_error");
            return new(null, error);
        }

        if (!string.Equals(request_record.client_id, authenticated_client_app, StringComparison.Ordinal))
        {
            log.LogWarning("OIDC_TOKEN_CODE_CLIENT_MISMATCH form_client_id={formClient} auth_client_id={authClient}", request_record.client_id, authenticated_client_app);
            return new(null, new("invalid_client", "client_id mismatch with authenticated client"));
        }

        var registeredClients = app.OIDCClientConfiguration;
        if (registeredClients == null || !registeredClients.TryGetValue(request_record.client_id, out var clientCfg))
        {
            log.LogWarning("OIDC_TOKEN_CODE_UNKNOWN_CLIENT client_id={clientId}", request_record.client_id);
            return new(null, new("invalid_client", "Unknown client_id"));
        }

        var sub_pepper = clientCfg.OIDCSubjectPepper;
        if (string.IsNullOrEmpty(sub_pepper))
        {
            log.LogWarning("OIDC_TOKEN_CODE_CLIENT_MISCONFIG client_id={clientId} missing=sub_pepper", request_record.client_id);
            return new(null, new("client_not_configured", "Missing SUBP"));
        }

        var record = codeService.ValidateAndConsumeAuthorizationCode(app, request_record.code, request_record.client_id, request_record.redirect_uri, request_record.code_verifier);
        if (record == null)
        {
            log.LogWarning("OIDC_TOKEN_CODE_INVALID client_id={clientId} redirect_uri={redirect} pkce_verifier_len={pkceLen}", request_record.client_id, request_record.redirect_uri, request_record.code_verifier?.Length ?? 0);
            return new(null, new("invalid_grant", "Invalid or expired authorization code / PKCE mismatch"));
        }

        var sid = OauthTokenServiceProvider.EnsureSID(record.Sid);
        var sub_hash = ApplicationOidcActiveSessions.GetDerivedSub(authenticated_client_app, record.UserId, sub_pepper);

        var (idTokenResult, accessTokenResult, tokenError) =
            await OauthTokenServiceProvider.GenerateAuthTokens(jwtHandler, app, sid, record.Nonce, request_record.client_id, sub_hash);

        if (tokenError != null || idTokenResult?.Token == null || accessTokenResult?.Token == null)
        {
            log.LogError("OIDC_TOKEN_CODE_GENERATE_FAILED client_id={clientId} sid={sid} error={error}", request_record.client_id, SidMarker(sid), tokenError?.Error ?? "token_generation_failed");
            return new(null, tokenError ?? new("server_error", "Failed to generate tokens"));
        }

        using var dbc = app.CreateDatabaseClient(log, deviceIdService, null);

        var (refresh_token, refresh_expiration, sub, upsert_error) = await OauthTokenServiceProvider.UpsertIdPSession(
            dbc,
            log,
            deviceIdService,
            encryptor,
            app,
            client_id: request_record.client_id,
            sid: sid,
            user_id: record.UserId,
            sub_pepper: sub_pepper,
            ct);

        var token_response = new OIDCTokenResponse(
            token_type: WellknownIdentityConstants.Bearer,
            expires_in: app.JWTTokenExpirationMinutes * 60,
            access_token: accessTokenResult.Token,
            refresh_token: refresh_token,
            id_token: idTokenResult.Token,
            scope: WellknownIdentityConstants.OpenID,
            refresh_expiration_utc: refresh_expiration.ToString("o")
        );

        log.LogInformation("OIDC_TOKEN_CODE_ISSUED client_id={clientId} sid={sid} user_id={userId} nonce_present={nonce} refresh_expiration={refreshExp:o} id_token_len={idLen} access_token_len={accLen} refresh_token_len={refLen} upsert_error={upsertErr}",
            request_record.client_id,
            SidMarker(sid),
            record.UserId,
            string.IsNullOrWhiteSpace(record.Nonce) ? "false" : "true",
            refresh_expiration,
            idTokenResult.Token.Length,
            accessTokenResult.Token.Length,
            refresh_token.Length,
            upsert_error?.Error ?? "none");

        return new(token_response, null);
    }

    public async Task<ResultWithStatus<OIDCTokenResponse, ErrorResult>> HandleTokenRequest(ApplicationOption app, IFormCollection form, string authenticated_client_app, CancellationToken ct)
    {
        var (code_request, refresh_request, error) = OauthTokenServiceProvider.ParseTokenRequest(form);

        if (error != null || (code_request == null && refresh_request == null))
        {
            log.LogWarning("OIDC_TOKEN_REQUEST_PARSE_FAILED client_auth={clientAuth} error={error}", authenticated_client_app, error?.Error ?? "parse_error");
            return new(null, error);
        }

        if (code_request != null)
        {
            return await HandleAuthTokenRequest(app, form, authenticated_client_app, ct);
        }
        else if (refresh_request != null)
        {
            return await HandleRefreshTokenGrant(app, form, authenticated_client_app, ct);
        }
        else
        {
            log.LogWarning("OIDC_TOKEN_UNSUPPORTED_GRANT client_id={clientId}", authenticated_client_app);
            return new(null, new("unsupported_grant_type", "Only authorization_code and refresh_token grants are supported"));
        }
    }
}
