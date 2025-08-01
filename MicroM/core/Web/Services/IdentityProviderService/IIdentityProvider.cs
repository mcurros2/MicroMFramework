using MicroM.Web.Authentication;

namespace MicroM.Web.Services;


/// <summary>
/// Defines the contract for an Identity Provider (IdP) that acts as an OpenID Connect server.
/// This interface handles all OIDC-specific tasks, such as client registration, token issuance,
/// and token validation.
/// </summary>
public interface IIdentityProvider
{
    /// <summary>
    /// Registers or updates the configuration for a client application (Service Provider)
    /// that will consume SSO from this IdP. This is a crucial security step to
    /// enforce allowed redirect URIs.
    /// </summary>
    /// <param name="clientAppId">The unique identifier for the client application.</param>
    /// <param name="redirectUris">A list of allowed redirect URIs for the client.</param>
    /// <param name="postLogoutRedirectUris">A list of allowed post-logout redirect URIs.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>True if the registration or update was successful, otherwise false.</returns>
    Task<bool> RegisterOrUpdateSsoClientAsync(
        string clientAppId,
        List<string> redirectUris,
        List<string> postLogoutRedirectUris,
        CancellationToken ct);

    /// <summary>
    /// Retrieves the configuration for a registered client application.
    /// </summary>
    /// <param name="clientAppId">The unique identifier for the client application.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The client's configuration, or null if no client is found with the given ID.</returns>
    Task<SSOClientConfiguration?> GetSsoClientConfigurationAsync(
        string clientAppId,
        CancellationToken ct);

    /// <summary>
    /// Validates a user's login credentials and creates a central user principal.
    /// This method is called by the OIDC Authorization endpoint after a user
    /// has submitted their credentials to the central login page.
    /// </summary>
    /// <param name="userLogin">The user's login credentials.</param>
    /// <param name="app_id">The application ID context for the login attempt.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A result object containing the authenticated principal or error details.</returns>
    Task<SSOAuthenticatorResult> AuthenticateUserLoginAsync(
        UserLogin userLogin,
        string app_id,
        CancellationToken ct);

    /// <summary>
    /// Generates a complete set of tokens (access, ID, and refresh) for a client application.
    /// This is used in the OIDC Token endpoint to exchange an authorization code for tokens.
    /// </summary>
    /// <param name="claims">A dictionary of user claims to be included in the tokens.</param>
    /// <param name="clientAppId">The client application's ID for which the tokens are generated.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A result object containing the generated tokens.</returns>
    Task<SSOTokenResult> GenerateTokensForClientAsync(
        Dictionary<string, object> claims,
        string clientAppId,
        CancellationToken ct);

    /// <summary>
    /// Renews an access token using a refresh token.
    /// This is used in the OIDC Token endpoint with a grant_type of "refresh_token".
    /// </summary>
    /// <param name="refreshToken">The refresh token to use for renewal.</param>
    /// <param name="clientAppId">The client application's ID.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A new set of tokens (access, ID, and refresh).</returns>
    Task<SSOTokenResult> RefreshTokensAsync(
        string refreshToken,
        string clientAppId,
        CancellationToken ct);

    /// <summary>
    /// Revokes a specific refresh token to invalidate a user's session from a client.
    /// </summary>
    /// <param name="refreshToken">The refresh token to revoke.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>True if the token was successfully revoked.</returns>
    Task<bool> RevokeRefreshTokenAsync(
        string refreshToken,
        CancellationToken ct);

    /// <summary>
    /// Validates an access token and returns a dictionary of its claims.
    /// This is used to protect APIs by ensuring the token is valid, unexpired, and correctly signed.
    /// </summary>
    /// <param name="accessToken">The access token to validate.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A dictionary of claims if the token is valid, otherwise null.</returns>
    Task<Dictionary<string, object>?> ValidateAccessTokenAsync(
        string accessToken,
        CancellationToken ct);

    /// <summary>
    /// Retrieves the public keys used by the IdP to sign its tokens.
    /// This endpoint is known as the "JSON Web Key Set" (JWKS) endpoint.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A JSON string representing the public key set.</returns>
    Task<string> GetJwksAsync(CancellationToken ct);
}
