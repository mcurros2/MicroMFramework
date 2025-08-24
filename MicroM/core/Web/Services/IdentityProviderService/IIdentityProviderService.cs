
namespace MicroM.Web.Services;

public interface IIdentityProviderService
{
    Task<Dictionary<string, string>> HandleWellKnown(string app_id, CancellationToken ct);

    Task<string> HandleJwks(string app_id, CancellationToken ct);

    Task<string> HandleAuthorizationCode(string app_id, string userId, CancellationToken ct);

    Task<string> HandleBackchannelToken(string app_id, string userId, CancellationToken ct);
}

