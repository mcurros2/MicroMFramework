using MicroM.Configuration;
using MicroM.Core;
using Microsoft.AspNetCore.Http;

namespace MicroM.Web.Authentication.SSO;

public interface IPushedAuthorizationService
{
    /// <summary>
    /// Create a pushed authorization request for the given app and client, storing the original parameters.
    /// Returns a pair (responseDictionary, errorObject). responseDictionary contains "request_uri" and "expires_in".
    /// </summary>
    ResultWithStatus<OIDCPARResponse, ErrorResult> CreatePushedRequest(ApplicationOption app, IFormCollection form, string authenticated_client_id);

    /// <summary>
    /// Retrieve the pushed request parameters by request_uri. Returns null if not found or expired.
    /// </summary>
    (PushedAuthorizationRequest? request, string? rawObject, JWTProtectedHeaderResult? header) ConsumeRequest(string requestUri);

    /// <summary>
    /// Remove a pushed request (one-time read).
    /// </summary>
    void RemoveRequest(string requestUri);

    bool RedirectUriMatches(string registered, string incoming);
}