using MicroM.Web.Authentication;
using MicroM.Web.Services;

namespace MicroM.Web.Controllers;

/// <summary>
/// Represents the IIdentityProviderController.
/// </summary>
public interface IIdentityProviderController
{
    /// <summary>
    /// Performs the WellKnown operation.
    /// </summary>
    Task<Dictionary<string, string>> WellKnown(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, CancellationToken ct);
    /// <summary>
    /// Performs the Jwks operation.
    /// </summary>
    Task<string> Jwks(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, CancellationToken ct);

    /// <summary>
    /// Performs the UserInfo operation.
    /// </summary>
    Task<Dictionary<string, object?>> UserInfo(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct);
    //Task<bool> Revoke(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string token, CancellationToken ct);
    //Task<Dictionary<string, object?>> Introspect(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string token, CancellationToken ct);

    /// <summary>
    /// Performs the Authorize operation.
    /// </summary>
    Task<string> Authorize(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct);
    /// <summary>
    /// Performs the Token operation.
    /// </summary>
    Task<string> Token(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct);

    /// <summary>
    /// Performs the PAR operation.
    /// </summary>
    Task<Dictionary<string, object?>> PAR(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string token, CancellationToken ct);

    /// <summary>
    /// Performs the EndSession operation.
    /// </summary>
    Task<bool> EndSession(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct);
}
