using MicroM.Configuration;
using MicroM.Core;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;

namespace MicroM.Web.Authentication.SSO;

public class OauthTokenService(IAuthorizationCodeService codeService, WebAPIJsonWebTokenHandler jwtHandler) : IOauthTokenService
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

    private ResultWithStatus<AuthorizationCodeRecord, ErrorResult> ValidateAuthenticatedClaims(ApplicationOption app, OauthTokenServiceRequestRecord request, string authenticated_client_id)
    {
        if (!string.Equals(request.client_id, authenticated_client_id, StringComparison.Ordinal))
        {
            return new(null, new("invalid_client", "client_id mismatch with authenticated client"));
        }

        var registeredClients = app.OIDCClientConfiguration;
        if (registeredClients == null || !registeredClients.TryGetValue(request.client_id, out var clientCfg))
        {
            return new(null, new("invalid_client", "Unknown client_id"));
        }

        var record = codeService.ValidateAndConsumeAuthorizationCode(app, request.code, request.client_id, request.redirect_uri, request.code_verifier);
        if (record == null)
        {
            return new(null, new("invalid_grant", "Invalid or expired authorization code / PKCE mismatch"));
        }

        return new(record, null);
    }


    public ResultWithStatus<OIDCTokenResponse, ErrorResult> HandleTokenRequest(ApplicationOption app, IFormCollection form, string authenticated_client_app)
    {
        var (request_record, error) = GetRequestRecord(form);

        if (error != null || request_record == null)
            return new(null, error);

        var validate_claims = ValidateAuthenticatedClaims(app, request_record, authenticated_client_app);
        if (validate_claims.Status != null || validate_claims.Result == null)
        {
            return new(null, validate_claims.Status);
        }

        var record = validate_claims.Result;

        var idClaims = new Dictionary<string, object>
        {
            [WellknownIdentityConstants.SubjectIdentifier] = record.UserId,
            [WellknownIdentityConstants.AuthorizedParty] = request_record.client_id
        };

        if (!string.IsNullOrEmpty(record.Sid))
        {
            idClaims[WellknownIdentityConstants.SessionIdentifier] = record.Sid;
        }

        if (!string.IsNullOrWhiteSpace(record.Nonce))
        {
            idClaims[WellknownIdentityConstants.Nonce] = record.Nonce;
        }

        var accessClaims = new Dictionary<string, object>
        {
            [WellknownIdentityConstants.SubjectIdentifier] = record.UserId,
            [WellknownIdentityConstants.AuthorizedParty] = request_record.client_id,
            [WellknownIdentityConstants.Scope] = "openid"
        };

        var idTokenResult = jwtHandler.GenerateJwtTokenWEBApi(idClaims, app);
        var accessTokenResult = jwtHandler.GenerateJwtTokenWEBApi(accessClaims, app);

        if (idTokenResult?.Token == null || accessTokenResult?.Token == null)
        {
            return new(null, new("server_error", "Failed to generate tokens"));
        }

        // Generate a durable refresh token (simple random value). Persist as needed by your authenticator/database.
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var refreshExpirationUtc = DateTimeOffset.UtcNow.AddHours(app.JWTRefreshExpirationHours).ToString("o");

        var token_response = new OIDCTokenResponse(
            token_type: "Bearer",
            expires_in: app.JWTTokenExpirationMinutes * 60,
            access_token: accessTokenResult.Token,
            refresh_token: refreshToken,
            id_token: idTokenResult.Token,
            scope: "openid",
            refresh_expiration_utc: refreshExpirationUtc
            );

        return new(token_response, null);
    }

}
