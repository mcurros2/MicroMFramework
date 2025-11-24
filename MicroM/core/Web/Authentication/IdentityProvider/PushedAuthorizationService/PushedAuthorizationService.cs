using MicroM.Configuration;
using MicroM.Core;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;

namespace MicroM.Web.Authentication.SSO;

public record PushedAuthorizationRequest(
    string? client_id,
    string? response_type,
    string? redirect_uri,
    string? scope,
    string? state,
    string? nonce,
    string? code_challenge,
    string? code_challenge_method,
    string? request
    );


public class PushedAuthorizationService : IPushedAuthorizationService
{
    // retain raw request object to allow authorize-time reconciliation & signature enforcement
    private record ParEntry(PushedAuthorizationRequest Request, string? RawRequestObject, DateTime ExpiresAt, JWTProtectedHeaderResult? header);

    private readonly ConcurrentDictionary<string, ParEntry> _store = new();

    // Default lifetime for a pushed request (seconds)
    private const int DEFAULT_EXPIRES_IN = 90;

    public ResultWithStatus<OIDCPARResponse, ErrorResult> CreatePushedRequest(ApplicationOption app, IFormCollection form, string authenticated_client_id)
    {
        var ((request, header), error) = PushedAuthorizationProvider.ValidateRequest(app, form);

        if (request == null || error != null)
        {
            return new(null, error);
        }

        request = request with { client_id = authenticated_client_id };

        if (app.OIDCClientConfiguration == null || !app.OIDCClientConfiguration.TryGetValue(request.client_id, out var clientCfg))
        {
            return new(null, new("invalid_client", "Unknown client_id"));
        }

        if (clientCfg.URLAuthorizedRedirects == null || clientCfg.URLAuthorizedRedirects.Count == 0)
        {
            return new(null, new("invalid_request", "Client has no registered redirect URIs"));
        }

        var matched = clientCfg.URLAuthorizedRedirects.Any(registered => RedirectUriMatches(registered, request.redirect_uri));
        if (!matched)
        {
            return new(null, new("invalid_request", "redirect_uri not registered for client"));
        }

        var requestUri = $"urn:ietf:params:oauth:request_uri:{CryptClass.GenerateBase64UrlRandomCode(32)}";
        var expiresAt = DateTime.UtcNow.AddSeconds(DEFAULT_EXPIRES_IN);

        string? rawRequestJwt = request.request;

        _store[requestUri] = new ParEntry(request, rawRequestJwt, expiresAt, header);

        var response = new OIDCPARResponse(request_uri: requestUri, expires_in: DEFAULT_EXPIRES_IN);

        return new(response, null);
    }

    // Retrieve full entry (including raw request object) for authorize reconciliation
    public (PushedAuthorizationRequest? request, string? rawObject, JWTProtectedHeaderResult? header) ConsumeRequest(string requestUri)
    {
        if (string.IsNullOrEmpty(requestUri)) return (null, null, null);
        if (!_store.TryGetValue(requestUri, out var entry)) return (null, null, null);
        _store.TryRemove(requestUri, out _);
        if (entry.ExpiresAt < DateTime.UtcNow) return (null, null, null);
        return (entry.Request, entry.RawRequestObject, entry.header);
    }

    public void RemoveRequest(string requestUri)
    {
        if (string.IsNullOrEmpty(requestUri)) return;
        _store.TryRemove(requestUri, out _);
    }

    public bool RedirectUriMatches(string registered, string incoming)
    {
        if (!Uri.TryCreate(registered, UriKind.Absolute, out var r)) return false;
        if (!Uri.TryCreate(incoming, UriKind.Absolute, out var i)) return false;

        if (!string.Equals(r.Scheme, i.Scheme, StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(r.Host, i.Host, StringComparison.OrdinalIgnoreCase)) return false;

        var rPort = r.IsDefaultPort ? (r.Scheme == "https" ? 443 : 80) : r.Port;
        var iPort = i.IsDefaultPort ? (i.Scheme == "https" ? 443 : 80) : i.Port;
        if (rPort != iPort) return false;

        var rPath = (r.AbsolutePath ?? string.Empty).TrimEnd('/');
        var iPath = (i.AbsolutePath ?? string.Empty).TrimEnd('/');
        if (rPath != iPath) return false;

        var rQuery = r.Query ?? string.Empty;
        var iQuery = i.Query ?? string.Empty;
        if (rQuery != iQuery) return false;

        return true;
    }
}
