using MicroM.Configuration;

namespace MicroM.Web.Authentication.SSO;

/// <summary>
/// Represents the IIdentityProviderService.
/// </summary>
public interface IIdentityProviderService
{
    /// <summary>
    /// Retrieves the OpenID Connect discovery document.
    /// </summary>
    /// <param name="app_config">Application configuration options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A dictionary containing discovery document values.</returns>
    Task<Dictionary<string, string>> GetWellKnown(ApplicationOption app_config, CancellationToken ct);

    /// <summary>
    /// Retrieves the JSON Web Key Set (JWKS).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A JSON Web Key Set.</returns>
    Task<string> GetJwks(CancellationToken ct);

    /// <summary>
    /// Requests an authorization code for the specified user.
    /// </summary>
    /// <param name="app_config">Application configuration options.</param>
    /// <param name="user_id">The user identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An authorization code.</returns>
    Task<string> GetAuthorizationCode(ApplicationOption app_config, string user_id, CancellationToken ct);

    /// <summary>
    /// Requests a backchannel token for the specified user.
    /// </summary>
    /// <param name="app_config">Application configuration options.</param>
    /// <param name="user_id">The user identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A backchannel token.</returns>
    Task<string> GetBackchannelToken(ApplicationOption app_config, string user_id, CancellationToken ct);
}
