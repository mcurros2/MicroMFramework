using MicroM.Configuration;
using MicroM.Core;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;

namespace MicroM.Web.Authentication.SSO;

public class PushedAuthorizationService : IPushedAuthorizationService
{
    private record ParEntry(PushedAuthorizationRequest Request, DateTime ExpiresAt);

    private readonly ConcurrentDictionary<string, ParEntry> _store = new();

    // Default lifetime for a pushed request (seconds)
    private const int DEFAULT_EXPIRES_IN = 90;

    public ResultWithStatus<OIDCPARResponse, ErrorResult> CreatePushedRequest(ApplicationOption app, IFormCollection form, string authenticated_client_id)
    {
        var (request, error) = PushedAuthorizationProvider.ValidateRequest(form);

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

        if (clientCfg.URLAuthorizedRedirects != null && clientCfg.URLAuthorizedRedirects.Count > 0)
        {
            var matched = clientCfg.URLAuthorizedRedirects.Any(registered => RedirectUriMatches(registered, request.redirect_uri));
            if (!matched)
            {
                return new(null, new("invalid_request", "redirect_uri not registered for client"));
            }
        }

        var requestUri = $"urn:ietf:params:oauth:request_uri:{CryptClass.GenerateBase64UrlRandomCode(32)}";
        var expiresAt = DateTime.UtcNow.AddSeconds(DEFAULT_EXPIRES_IN);

        _store[requestUri] = new ParEntry(request, expiresAt);

        var response = new OIDCPARResponse(request_uri: requestUri, expires_in: DEFAULT_EXPIRES_IN);

        return new(response, null);
    }

    public PushedAuthorizationRequest? ConsumeRequest(string requestUri)
    {
        if (string.IsNullOrEmpty(requestUri)) return null;

        if (!_store.TryGetValue(requestUri, out var entry)) return null;

        _store.TryRemove(requestUri, out _);

        if (entry.ExpiresAt < DateTime.UtcNow)
        {
            return null;
        }

        return entry.Request;
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
        if (!string.Equals(rPath, iPath, StringComparison.Ordinal)) return false;

        var rQuery = r.Query ?? string.Empty;
        var iQuery = i.Query ?? string.Empty;
        if (!string.Equals(rQuery, iQuery, StringComparison.Ordinal)) return false;

        return true;
    }
}
