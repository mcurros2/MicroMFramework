using MicroM.Web.Services;

namespace MicroM.Web.Controllers;

public interface IIdentityProviderClientController
{
    Task<string> Jwks(IMicroMAppConfiguration app_config, string app_id, CancellationToken ct);
    Task SignInOidc(string app_id, string? returnUrl, CancellationToken ct);
    Task SignOutOidc(string app_id, string? returnUrl, CancellationToken ct);
}
