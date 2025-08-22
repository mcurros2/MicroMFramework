
namespace MicroM.Web.Services;

/// <summary>
/// Represents the IIdentityProviderService.
/// </summary>
public interface IIdentityProviderService
{
    /// <summary>
    /// Performs the HandleWellKnown operation.
    /// </summary>
    Task<Dictionary<string, string>> HandleWellKnown(string app_id, CancellationToken ct);

    /// <summary>
    /// Performs the HandleJwks operation.
    /// </summary>
    Task<string> HandleJwks(string app_id, CancellationToken ct);

    /// <summary>
    /// Performs the HandleAuthorizationCode operation.
    /// </summary>
    Task<string> HandleAuthorizationCode(string app_id, string userId, CancellationToken ct);

    /// <summary>
    /// Performs the HandleBackchannelToken operation.
    /// </summary>
    Task<string> HandleBackchannelToken(string app_id, string userId, CancellationToken ct);
}

