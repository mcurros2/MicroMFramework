using MicroM.Configuration;
using MicroM.Web.Authentication.SSO;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal sealed class IdPDiagnosticsContext(
    ApplicationOption app,
    IOIDCHttpClient httpClient)
{
    public ApplicationOption App { get; } = app;
    public IOIDCHttpClient HttpClient { get; } = httpClient;
}