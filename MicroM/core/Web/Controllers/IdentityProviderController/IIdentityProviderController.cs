using MicroM.Web.Authentication;
using MicroM.Web.Services;

namespace MicroM.Web.Controllers;

/// <summary>
/// Endpoints supporting OpenID Connect/OAuth2 flows.
/// </summary>
public interface IIdentityProviderController
{
    /// <summary>
    /// Publishes discovery metadata so clients can locate identity endpoints.
    /// </summary>
    /// <param name="auth">Authentication provider supplying issuer details.</param>
    /// <param name="app_config">Configuration governing identity settings.</param>
    /// <param name="app_id">Identifier for the target application.</param>
    /// <param name="ct">Token to cancel the request.</param>
    /// <returns>A dictionary describing available endpoints and capabilities.</returns>
    /// <remarks>Not yet implemented.</remarks>
    Task<Dictionary<string, string>> WellKnown(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, CancellationToken ct);
    /// <summary>
    /// Exposes the JSON Web Key Set (JWKS) for token validation.
    /// </summary>
    /// <param name="auth">Authentication provider containing signing keys.</param>
    /// <param name="app_config">Configuration referencing key material.</param>
    /// <param name="app_id">Identifier for the target application.</param>
    /// <param name="ct">Token to cancel the request.</param>
    /// <returns>A JSON string containing the public keys.</returns>
    /// <remarks>Not yet implemented.</remarks>
    Task<string> Jwks(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, CancellationToken ct);

    /// <summary>
    /// Provides claims for an authenticated user per OpenID Connect.
    /// </summary>
    /// <param name="auth">Authentication provider retrieving user claims.</param>
    /// <param name="app_config">Configuration specifying claim issuance rules.</param>
    /// <param name="app_id">Identifier for the requesting application.</param>
    /// <param name="userId">Identifier of the subject whose information is requested.</param>
    /// <param name="ct">Token to cancel the request.</param>
    /// <returns>A dictionary containing user claims.</returns>
    /// <remarks>Not yet implemented.</remarks>
    Task<Dictionary<string, object?>> UserInfo(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct);
    //Task<bool> Revoke(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string token, CancellationToken ct);
    //Task<Dictionary<string, object?>> Introspect(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string token, CancellationToken ct);

    /// <summary>
    /// Initiates an OAuth2 authorization request.
    /// </summary>
    /// <param name="auth">Authentication provider evaluating client and user.</param>
    /// <param name="app_config">Application configuration defining grant settings.</param>
    /// <param name="app_id">Identifier for the client application.</param>
    /// <param name="userId">Identifier for the authenticated user.</param>
    /// <param name="ct">Token to cancel the request.</param>
    /// <returns>A serialized authorization response.</returns>
    /// <remarks>Not yet implemented.</remarks>
    Task<string> Authorize(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct);
    /// <summary>
    /// Issues access tokens based on an authorization grant.
    /// </summary>
    /// <param name="auth">Authentication provider validating the grant.</param>
    /// <param name="app_config">Configuration supplying token parameters.</param>
    /// <param name="app_id">Identifier for the requesting application.</param>
    /// <param name="userId">Identifier for the user associated with the request.</param>
    /// <param name="ct">Token to cancel the request.</param>
    /// <returns>A serialized token response containing issued tokens.</returns>
    /// <remarks>Not yet implemented.</remarks>
    Task<string> Token(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct);

    /// <summary>
    /// Handles pushed authorization requests (PAR).
    /// </summary>
    /// <param name="auth">Authentication provider validating the request token.</param>
    /// <param name="app_config">Configuration governing PAR support.</param>
    /// <param name="app_id">Identifier for the relying party.</param>
    /// <param name="token">Token referencing the pushed authorization request.</param>
    /// <param name="ct">Token to cancel the request.</param>
    /// <returns>A dictionary containing a request URI or related response fields.</returns>
    /// <remarks>Not yet implemented.</remarks>
    Task<Dictionary<string, object?>> PAR(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string token, CancellationToken ct);

    /// <summary>
    /// Ends the user's session and signals clients to clear tokens.
    /// </summary>
    /// <param name="auth">Authentication provider handling session revocation.</param>
    /// <param name="app_config">Configuration controlling logout behavior.</param>
    /// <param name="app_id">Identifier of the application requesting logout.</param>
    /// <param name="userId">Identifier of the user ending the session.</param>
    /// <param name="ct">Token to cancel the request.</param>
    /// <returns><see langword="true"/> when the session is ended.</returns>
    /// <remarks>Not yet implemented.</remarks>
    Task<bool> EndSession(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct);
}
