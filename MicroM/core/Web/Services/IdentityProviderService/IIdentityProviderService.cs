
namespace MicroM.Web.Services;

/// <summary>
/// Represents the IIdentityProviderService.
/// </summary>
public interface IIdentityProviderService
{
    /// <summary>
    /// Returns discovery metadata for the identity provider.
    /// </summary>
    /// <param name="app_id">Application identifier.</param>
    /// <param name="ct">Token to observe for cancellation.</param>
    /// <returns>A dictionary containing well-known configuration values.</returns>
    /// <remarks>Throws <see cref="OperationCanceledException"/> when cancelled.</remarks>
    Task<Dictionary<string, string>> HandleWellKnown(string app_id, CancellationToken ct);

    /// <summary>
    /// Retrieves the JSON Web Key Set (JWKS) for the identity provider.
    /// </summary>
    /// <param name="app_id">Application identifier.</param>
    /// <param name="ct">Token to observe for cancellation.</param>
    /// <returns>The JWKS document as a JSON string.</returns>
    /// <remarks>Throws <see cref="OperationCanceledException"/> when cancelled.</remarks>
    Task<string> HandleJwks(string app_id, CancellationToken ct);

    /// <summary>
    /// Handles generation of an authorization code for the specified user.
    /// </summary>
    /// <param name="app_id">Application identifier.</param>
    /// <param name="userId">User for whom the code is generated.</param>
    /// <param name="ct">Token to observe for cancellation.</param>
    /// <returns>The generated authorization code.</returns>
    /// <remarks>Throws <see cref="OperationCanceledException"/> when cancelled.</remarks>
    Task<string> HandleAuthorizationCode(string app_id, string userId, CancellationToken ct);

    /// <summary>
    /// Issues a back-channel authentication token for the specified user.
    /// </summary>
    /// <param name="app_id">Application identifier.</param>
    /// <param name="userId">User requesting the token.</param>
    /// <param name="ct">Token to observe for cancellation.</param>
    /// <returns>The generated token string.</returns>
    /// <remarks>Throws <see cref="OperationCanceledException"/> when cancelled.</remarks>
    Task<string> HandleBackchannelToken(string app_id, string userId, CancellationToken ct);
}

