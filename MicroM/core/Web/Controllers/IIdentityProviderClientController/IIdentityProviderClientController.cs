namespace MicroM.Web.Controllers;

public interface IIdentityProviderClientController
{
    Task SignInOidc(string app_id, string? returnUrl, CancellationToken ct);
    Task SignOutOidc(string app_id, string? returnUrl, CancellationToken ct);
}
