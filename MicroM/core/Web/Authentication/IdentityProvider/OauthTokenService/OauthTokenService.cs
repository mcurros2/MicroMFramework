using MicroM.Configuration;
using MicroM.Core;
using MicroM.DataDictionary.Entities;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace MicroM.Web.Authentication.SSO;

public class OauthTokenService(
    IAuthorizationCodeService codeService,
    WebAPIJsonWebTokenHandler jwtHandler,
    IDeviceIdService deviceIdService,
    IMicroMEncryption encryptor,
    ILogger<OauthTokenService> log) : IOauthTokenService
{

    private static ResultWithStatus<OauthTokenServiceRequestRecord, ErrorResult> GetRequestRecord(IFormCollection form)
    {
        string? grant_type = form["grant_type"];
        string? code = form["code"];
        string? redirect_uri = form["redirect_uri"];
        string? code_verifier = form["code_verifier"];
        string? client_id = form["client_id"];


        if (string.IsNullOrEmpty(grant_type) || grant_type != "authorization_code")
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
                grant_type: form["grant_type"].ToString(),
                code: form["code"].ToString(),
                redirect_uri: form["redirect_uri"].ToString(),
                code_verifier: form["code_verifier"].ToString(),
                client_id: form["client_id"].ToString()
                ),
                null
            );
    }

    public async Task<ResultWithStatus<OIDCTokenResponse, ErrorResult>> HandleTokenRequest(ApplicationOption app, IFormCollection form, string authenticated_client_app)
    {
        var (request_record, error) = GetRequestRecord(form);

        if (error != null || request_record == null)
            return new(null, error);


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

        var sub_hash = ApplicationOidcActiveSessions.GetDerivedSub(authenticated_client_app, record.UserId, sub_pepper);

        // Ensure sid (reuse from authorize or generate)
        var sid = string.IsNullOrWhiteSpace(record.Sid) ? Guid.NewGuid().ToString() : record.Sid;

        var idClaims = new Dictionary<string, object>
        {
            [WellknownIdentityConstants.SubjectIdentifier] = sub_hash,
            [WellknownIdentityConstants.AuthorizedParty] = request_record.client_id,
            [WellknownIdentityConstants.SessionIdentifier] = sid
        };

        if (!string.IsNullOrWhiteSpace(record.Nonce))
        {
            idClaims[WellknownIdentityConstants.Nonce] = record.Nonce;
        }

        var accessClaims = new Dictionary<string, object>
        {
            [WellknownIdentityConstants.SubjectIdentifier] = sub_hash,
            [WellknownIdentityConstants.AuthorizedParty] = request_record.client_id,
            [WellknownIdentityConstants.Scope] = "openid"
        };

        var idTokenResult = jwtHandler.GenerateJwtTokenWEBApi(idClaims, app, audience: request_record.client_id);
        var accessTokenResult = jwtHandler.GenerateJwtTokenWEBApi(accessClaims, app);

        if (idTokenResult?.Token == null || accessTokenResult?.Token == null)
        {
            return new(null, new("server_error", "Failed to generate tokens"));
        }

        // IdP OIDC refresh token (subject-wide, not per client-device)
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var refreshExpirationUtc = DateTimeOffset.UtcNow.AddHours(app.JWTRefreshExpirationHours);

        using var dbc = app.CreateDatabaseClient(log, deviceIdService, null);
        try
        {
            await dbc.Connect(CancellationToken.None);

            // Use a stable sentinel for device_id (token is backchannel; no browser device here)

            var persistedSid = await ApplicationOidcActiveSessions.CreateIdPSession(
                dbc,
                client_app_id: request_record.client_id,
                username: null,
                idp_user_id: record.UserId,
                device_id: WellknownIdentityConstants.oidc,
                subject_pepper: sub_pepper,
                encryptor: encryptor,
                ct: CancellationToken.None,
                existing_session_id: sid,
                idp_refresh_token: refreshToken,
                refresh_expiration_utc: refreshExpirationUtc.UtcDateTime
            );
        }
        catch (Exception ex)
        {
            // Do not fail token issuance on persistence errors
            log.LogError(ex, "IdP token: failed to persist client session (client_id={client}, user_id={user}, sid={sid})", request_record.client_id, record.UserId, sid);
        }
        finally
        {
            await dbc.Disconnect();
        }

        var token_response = new OIDCTokenResponse(
            token_type: "Bearer",
            expires_in: app.JWTTokenExpirationMinutes * 60,
            access_token: accessTokenResult.Token,
            refresh_token: refreshToken,
            id_token: idTokenResult.Token,
            scope: "openid",
            refresh_expiration_utc: refreshExpirationUtc.ToString("o")
            );

        return new(token_response, null);
    }

}
