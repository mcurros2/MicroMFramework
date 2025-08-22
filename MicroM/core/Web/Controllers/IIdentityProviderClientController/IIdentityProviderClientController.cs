namespace MicroM.Web.Controllers;

/// <summary>
/// Defines the contract for client-side identity provider interactions.
/// </summary>
public interface IIdentityProviderClientController
{
    /// <summary>
    /// Initiates the OpenID Connect sign-in flow.
    /// </summary>
    Task SignInOidc(string app_id, string? returnUrl, CancellationToken ct);
    /// <summary>
    /// Initiates the OpenID Connect sign-out flow.
    /// </summary>
    Task SignOutOidc(string app_id, string? returnUrl, CancellationToken ct);
}
