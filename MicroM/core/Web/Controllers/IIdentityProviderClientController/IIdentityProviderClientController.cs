namespace MicroM.Web.Controllers;

/// <summary>
/// Represents the IIdentityProviderClientController.
/// </summary>
public interface IIdentityProviderClientController
{
    /// <summary>
    /// Performs the SignInOidc operation.
    /// </summary>
    Task SignInOidc(string app_id, string? returnUrl, CancellationToken ct);
    /// <summary>
    /// Performs the SignOutOidc operation.
    /// </summary>
    Task SignOutOidc(string app_id, string? returnUrl, CancellationToken ct);
}
