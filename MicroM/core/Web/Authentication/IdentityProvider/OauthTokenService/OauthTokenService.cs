using MicroM.Configuration;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;

namespace MicroM.Web.Authentication.SSO;

public class OauthTokenService(IAuthorizationCodeService codeService, WebAPIJsonWebTokenHandler jwtHandler) : IOauthTokenService
{

    private static (OauthTokenServiceRequestRecord? request_record, object? error) GetRequestRecord(IFormCollection form)
    {

        OauthTokenServiceRequestRecord request_record = new(
                grant_type: form["grant_type"],
                code: form["code"],
                redirect_uri: form["redirect_uri"],
                code_verifier: form["code_verifier"],
                client_id: form["client_id"]
        );

        if (request_record.grant_type != "authorization_code")
        {
            return (request_record: null!, error: new { error = "unsupported_grant_type", error_description = "Only authorization_code is supported" });
        }

        if (string.IsNullOrEmpty(request_record.code_verifier))
        {
            return (request_record: null!, error: new { error = "invalid_request", error_description = "Missing or invalid 'code_verifier' parameter." });
        }

        if (string.IsNullOrEmpty(request_record.code))
        {
            return (request_record: null!, error: new { error = "invalid_request", error_description = "Missing or invalid 'code' parameter." });
        }

        if (string.IsNullOrEmpty(request_record.redirect_uri))
        {
            return (request_record: null!, error: new { error = "invalid_request", error_description = "Missing or invalid 'redirect_uri' parameter." });
        }

        if (string.IsNullOrEmpty(request_record.client_id))
        {
            return (request_record: null!, error: new { error = "invalid_request", error_description = "Missing or invalid 'client_id' parameter." });
        }

        return
            (
                request_record: new
                (
                grant_type: form["grant_type"].ToString(),
                code: form["code"].ToString(),
                redirect_uri: form["redirect_uri"].ToString(),
                code_verifier: form["code_verifier"].ToString(),
                client_id: form["client_id"].ToString()
                ),
                error: null
            );
    }

    private (AuthorizationCodeRecord? record, object? error) ValidateAuthenticatedClaims(ApplicationOption app, OauthTokenServiceRequestRecord request, string authenticated_client_id)
    {
        if (!string.Equals(request.client_id, authenticated_client_id, StringComparison.Ordinal))
        {
            return (null, new { error = "invalid_client", error_description = "client_id mismatch with authenticated client" });
        }

        var registeredClients = app.OIDCClientConfiguration;
        if (registeredClients == null || !registeredClients.TryGetValue(request.client_id, out var clientCfg))
        {
            return (null, new { error = "invalid_client", error_description = "Unknown client_id" });
        }

        var record = codeService.ValidateAndConsumeAuthorizationCode(app, request.code, request.client_id, request.redirect_uri, request.code_verifier);
        if (record == null)
        {
            return (null, new { error = "invalid_grant", error_description = "Invalid or expired authorization code / PKCE mismatch" });
        }

        return (record, null);
    }


    public (OIDCTokenResponse? response, object? error) HandleTokenRequest(ApplicationOption app, IFormCollection form, string authenticated_client_app)
    {
        var (request_record, error) = GetRequestRecord(form);

        if (error != null || request_record == null)
            return (response: null, error);

        var result = ValidateAuthenticatedClaims(app, request_record, authenticated_client_app);
        if (result.error != null || result.record == null)
        {
            return (null, result.error);
        }

        var record = result.record;

        var idClaims = new Dictionary<string, object>
        {
            ["sub"] = record.UserId,
            ["azp"] = request_record.client_id
        };

        if (!string.IsNullOrEmpty(record.Sid))
        {
            idClaims["sid"] = record.Sid!;
        }

        var accessClaims = new Dictionary<string, object>
        {
            ["sub"] = record.UserId,
            ["azp"] = request_record.client_id,
            ["scope"] = "openid"
        };

        var idTokenResult = jwtHandler.GenerateJwtTokenWEBApi(idClaims, app);
        var accessTokenResult = jwtHandler.GenerateJwtTokenWEBApi(accessClaims, app);

        if (idTokenResult?.Token == null || accessTokenResult?.Token == null)
        {
            return (null, new { error = "server_error", error_description = "Failed to generate tokens" });
        }

        // Generate a durable refresh token (simple random value). Persist as needed by your authenticator/database.
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var token_response = new OIDCTokenResponse(
            token_type: "Bearer",
            expires_in: app.JWTTokenExpirationMinutes * 60,
            access_token: accessTokenResult.Token,
            refresh_token: refreshToken,
            id_token: idTokenResult.Token,
            scope: "openid"
            );

        return (response: token_response, error: null);
    }

}
