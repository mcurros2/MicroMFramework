using MicroM.Configuration.CategoriesDefinitions;
using MicroM.Core;
using MicroM.Data;
using MicroM.Diagnostics;
using MicroM.Web.Authentication.SSO;
using MicroM.Web.Services;

namespace MicroM.Configuration.Entities;

public record APPOidcDiagnosticsResult : EntityActionResult
{
    public Dictionary<string, List<DiagnosticResult>> DiagnosticsResult { get; set; } = [];
}

public class APPOIDCDiagnostics : EntityActionBase
{

    public override async Task<EntityActionResult> Execute(EntityBase entity, DataWebAPIRequest parms, EntityDefinition def, MicroMOptions? Options, IWebAPIServices? API, IMicroMEncryption? encryptor, string? app_id, CancellationToken ct)
    {
        var result = new APPOidcDiagnosticsResult();
        var c_application_id = parms.Values[nameof(ApplicationsDef.c_application_id)].ToString();

        if (c_application_id == null || API == null)
        {
            result.DiagnosticsResult["app_config"] = [new("Application Lookup", Errors: [new("c_application_id_missing", "App ID missing")])];
            return result;
        }

        var app = API.app_config.GetAppConfiguration(c_application_id);

        if (app == null)
        {
            result.DiagnosticsResult["app_config"] = [new("Application Lookup", Errors: [new("app_not_found", "App not found")])];
            return result;
        }

        if (app.IdentityProviderRoleType == null || app.IdentityProviderRoleType == nameof(IdentityProviderRole.IDPDisabled))
        {
            result.DiagnosticsResult["app_config"] = [new("Provider Role", Errors: [new("app_invalid_role", "Application is not not configured for OIDC")])];
            return result;
        }

        if (app.IdentityProviderRoleType == nameof(IdentityProviderRole.IDPServer))
        {
            var serverDiagnostics = new OIDCIdPDiagnostics(API.app_config, API.oidcHttpClient);
            result.DiagnosticsResult = await serverDiagnostics.RunAllDiagnosticsAsync(c_application_id, ct);
        }
        else if (app.IdentityProviderRoleType == nameof(IdentityProviderRole.IDPClient))
        {
            var clientDiagnostics = new OIDCClientDiagnostics(API.app_config, API.oidcHttpClient);
            result.DiagnosticsResult = await clientDiagnostics.RunAllDiagnosticsAsync(c_application_id, ct);
        }
        else
        {
            result.DiagnosticsResult["app_config"] = [new("Provider Role", Errors: [new("app_invalid_role", "Application has an invalid OIDC role configuration")])];
            return result;
        }

        return result;
    }
}
