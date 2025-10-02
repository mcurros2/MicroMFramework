using MicroM.Configuration;
using Microsoft.AspNetCore.Http;

namespace MicroM.Web.Authentication.SSO;

public static class AuthorizeEndpointProvider
{

    private static OIDCAuthorizeRequest GetAuthorizeRequest(IQueryCollection query_string)
    {
        OIDCAuthorizeRequest auth_request = new
        (
            response_type: query_string["response_type"],
            client_id: query_string["client_id"],
            redirect_uri: query_string["redirect_uri"],
            scope: query_string["scope"],
            state: query_string["state"],
            nonce: query_string["nonce"],
            display: query_string["display"],
            prompt: query_string["prompt"],
            max_age: query_string["max_age"],
            ui_locales: query_string["ui_locales"],
            id_token_hint: query_string["id_token_hint"],
            login_hint: query_string["login_hint"],
            acr_values: query_string["acr_values"]
        );

        return auth_request;
    }

    public static (OIDCAuthorizeRequest? request, object? error) ValidateAndOverrideWithPARAuthorizationRequest(ApplicationOption app, IPushedAuthorizationService par_service, IQueryCollection query_string)
    {
        var qs = GetAuthorizeRequest(query_string);

        // PAR
        PushedAuthorizationRequest? pushed = null;
        if (!string.IsNullOrEmpty(qs.request_uri))
        {
            pushed = par_service.ConsumeRequest(qs.request_uri);
            if (pushed == null)
            {
                return (null, new { error = "invalid_request", error_description = "request_uri not found or expired" });
            }

            qs = new
            (
                client_id: pushed.client_id,
                response_type: pushed.response_type,
                redirect_uri: pushed.redirect_uri,
                state: string.IsNullOrEmpty(qs.state) ? pushed.state : qs.state,
                scope: pushed.scope,
                code_challenge: pushed.code_challenge,
                code_challenge_method: pushed.code_challenge_method
            );
        }

        // Basic validation
        if (string.IsNullOrEmpty(qs.response_type) || !string.Equals(qs.response_type, "code", StringComparison.OrdinalIgnoreCase))
        {
            return (null, new { error = "unsupported_response_type", error_description = "response_type must be 'code'" });
        }

        if (string.IsNullOrEmpty(qs.client_id))
        {
            return (null, new { error = "invalid_request", error_description = "client_id is required" });
        }

        if (string.IsNullOrEmpty(qs.redirect_uri))
        {
            return (null, new { error = "invalid_request", error_description = "redirect_uri is required" });
        }

        // Validate client registration and redirect_uri
        if (app.OIDCClientConfiguration == null || !app.OIDCClientConfiguration.TryGetValue(qs.client_id, out var clientCfg))
        {
            return (null, new { error = "invalid_client", error_description = "Unknown client_id" });
        }

        if (clientCfg.URLAuthorizedRedirects == null || clientCfg.URLAuthorizedRedirects.Count == 0)
        {
            return (null, new { error = "invalid_request", error_description = "Client has no registered redirect URIs" });
        }

        var matched = clientCfg.URLAuthorizedRedirects.Any(registered => par_service.RedirectUriMatches(registered, qs.redirect_uri));
        if (!matched)
        {
            return (null, new { error = "invalid_request", error_description = "redirect_uri not registered for client" });
        }

        return (qs, null);
    }

    public static string BuildLoginURL(ApplicationOption app, IQueryCollection query, string request_base)
    {
        // build a login URL. Prefer FrontendURLS[0] if available, otherwise fallback to request_base + "/login"
        var frontend = app.FrontendURLS != null && app.FrontendURLS.Count > 0 ? app.FrontendURLS[0] : null;
        string loginBase = !string.IsNullOrEmpty(frontend) ? frontend.TrimEnd('/') : request_base.TrimEnd('/');

        // Recreate original authorize URL to return to after login
        var q = System.Web.HttpUtility.ParseQueryString(string.Empty);
        foreach (var k in query.Keys) q[k] = query[k].ToString();
        var returnTo = $"{request_base}/oauth2/authorize?{q}";

        var loginUrl = $"{loginBase}/login?return_to={Uri.EscapeDataString(returnTo)}";
        return loginUrl;
    }

    public static string BuildRedirectURI(string redirect_uri, string? state, string code)
    {
        var separator = redirect_uri.Contains('?') ? '&' : '?';
        var redirectWithCode = $"{redirect_uri}{separator}code={Uri.EscapeDataString(code)}";
        if (!string.IsNullOrEmpty(state))
        {
            redirectWithCode += $"&state={Uri.EscapeDataString(state)}";
        }
        return redirectWithCode;
    }
}
