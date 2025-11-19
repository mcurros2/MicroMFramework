using MicroM.Configuration;
using MicroM.Core;
using MicroM.DataDictionary.Entities;
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

    private async Task<ResultWithStatus<OIDCTokenResponse, ErrorResult>> HandleRefreshTokenGrant(ApplicationOption app, IFormCollection form, string authenticated_client_app, CancellationToken ct)
    {
        var (record, error) = OauthTokenServiceProvider.GetRefreshTokenRequestRecord(form);

        if (record == null || error != null)
        {
            return new(null, error);
        }

        if (!string.Equals(record.client_id, authenticated_client_app, StringComparison.OrdinalIgnoreCase))
        {
            return new(null, new("invalid_client", "client_id mismatch with authenticated client"));
        }

        var registeredClients = app.OIDCClientConfiguration;
        if (registeredClients == null || !registeredClients.TryGetValue(record.client_id, out var clientCfg))
        {
            return new(null, new("invalid_client", "Unknown client_id"));
        }

        var sub_pepper = clientCfg.OIDCSubjectPepper;
        if (string.IsNullOrEmpty(sub_pepper))
        {
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
                return new(null, new("invalid_grant", "Invalid refresh token"));
            }

            // Validate expiration
            if (session.dt_refresh_expiration == null || session.dt_refresh_expiration <= DateTime.UtcNow)
            {
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

            var (idTokenResult, accessTokenResult, tokenError) = await OauthTokenServiceProvider.GenerateAuthTokens(jwtHandler, app, session.vc_oidc_session_id, null, record.client_id, sub!);

            if (idTokenResult?.Token == null || accessTokenResult?.Token == null)
            {
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
            return new(null, error);
        }

        if (!string.Equals(request_record.client_id, authenticated_client_app, StringComparison.Ordinal))
        {
            return new(null, new("invalid_client", "client_id mismatch with authenticated client"));
        }

        var registeredClients = app.OIDCClientConfiguration;
        if (registeredClients == null || !registeredClients.TryGetValue(request_record.client_id, out var clientCfg))
        {
            return new(null, new("invalid_client", "Unknown client_id"));
        }

        var sub_pepper = clientCfg.OIDCSubjectPepper;
        if (string.IsNullOrEmpty(sub_pepper))
        {
            return new(null, new("client_not_configured", "Missing SUBP"));
        }

        var record = codeService.ValidateAndConsumeAuthorizationCode(app, request_record.code, request_record.client_id, request_record.redirect_uri, request_record.code_verifier);
        if (record == null)
        {
            return new(null, new("invalid_grant", "Invalid or expired authorization code / PKCE mismatch"));
        }

        // Ensure a non-null sid for both tokens and persistence
        var sid = OauthTokenServiceProvider.EnsureSID(record.Sid);

        var sub_hash = ApplicationOidcActiveSessions.GetDerivedSub(authenticated_client_app, record.UserId, sub_pepper);

        var (idTokenResult, accessTokenResult, tokenError) = await OauthTokenServiceProvider.GenerateAuthTokens(jwtHandler, app, sid, record.Nonce, request_record.client_id, sub_hash);

        if (tokenError != null)
        {
            return new(null, tokenError);
        }

        using var dbc = app.CreateDatabaseClient(log, deviceIdService, null);

        // This will log error but continue token issuance
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
            access_token: accessTokenResult!.Token!,
            refresh_token: refresh_token,
            id_token: idTokenResult!.Token,
            scope: WellknownIdentityConstants.OpenID,
            refresh_expiration_utc: refresh_expiration.ToString("o")
            );

        return new(token_response, null);
    }

    public async Task<ResultWithStatus<OIDCTokenResponse, ErrorResult>> HandleTokenRequest(ApplicationOption app, IFormCollection form, string authenticated_client_app, CancellationToken ct)
    {
        var (code_request, refresh_request, error) = OauthTokenServiceProvider.ParseTokenRequest(form);

        if (error != null || (code_request == null && refresh_request == null))
        {
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
            return new(null, new("unsupported_grant_type", "Only authorization_code and refresh_token grants are supported"));
        }
    }


}
