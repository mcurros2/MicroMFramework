using MicroM.Web.Authentication;
using MicroM.Web.Services;

namespace MicroM.Web.Controllers;

/// <summary>
/// Defines the contract for OpenID Connect and OAuth2 endpoints.
/// </summary>
public interface IIdentityProviderController
{
    /// <summary>
    /// Returns the OpenID Connect discovery document.
    /// </summary>
    Task<Dictionary<string, string>> WellKnown(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, CancellationToken ct);
    /// <summary>
    /// Returns the JSON Web Key Set used to validate tokens.
    /// </summary>
    Task<string> Jwks(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, CancellationToken ct);

    /// <summary>
    /// Returns claims about the authenticated user.
    /// </summary>
    Task<Dictionary<string, object?>> UserInfo(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct);
    //Task<bool> Revoke(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string token, CancellationToken ct);
    //Task<Dictionary<string, object?>> Introspect(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string token, CancellationToken ct);

    /// <summary>
    /// Handles an authorization request.
    /// </summary>
    Task<string> Authorize(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct);
    /// <summary>
    /// Issues tokens for an authorization request.
    /// </summary>
    Task<string> Token(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct);

    /// <summary>
    /// Processes a pushed authorization request.
    /// </summary>
    Task<Dictionary<string, object?>> PAR(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string token, CancellationToken ct);

    /// <summary>
    /// Terminates a user's session and revokes tokens.
    /// </summary>
    Task<bool> EndSession(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct);
}
