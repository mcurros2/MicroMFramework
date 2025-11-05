using MicroM.Configuration;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.Diagnostics;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal static class ClientDiagnosticsProvider
{
    internal static DiagnosticResult? ValidateApp(string test_name, ApplicationOption? app)
    {
        if (app == null)
            return new(test_name, Errors: [new("app_not_found", "Application configuration not found")]);
        if (app.IdentityProviderRoleType != nameof(IdentityProviderRole.IDPClient))
            return new(test_name, Errors: [new("invalid_role", "Application is not configured as IDPClient")]);
        if (string.IsNullOrWhiteSpace(app.OIDCWellKnownURL))
            return new(test_name, Errors: [new("url_missing", $"OIDC Well Known URL is not configured")]);

        return null;
    }
}
