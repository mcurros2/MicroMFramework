using MicroM.Configuration;
using MicroM.Web.Authentication.SSO;
using System.Text.Json;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal class ClientDiagnosticsContext(
    IOIDCHttpClient httpClient,
    ApplicationOption app
    )
{
    public readonly IOIDCHttpClient HttpClient = httpClient;
    public readonly ApplicationOption App = app;

    // Well known document
    public JsonDocument? wellKnownDoc = null;

    // OIDC Well known endpoints
    public string? parURL = null;
    public string? authorizeURL = null;
    public string? tokenURL = null;
    public string? userInfoURL = null;
    public string? revocationURL = null;
    public string? introspectionURL = null;
    public string? endSessionURL = null;
    public string? idpRefreshURL = null;
}
