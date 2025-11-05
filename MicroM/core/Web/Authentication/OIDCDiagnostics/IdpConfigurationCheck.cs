using MicroM.Core;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.Diagnostics;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal sealed class IdpConfigurationCheck : IDiagnosticCheck<IdPDiagnosticsContext>
{
    public string DiagnosticId => "oidc_idp_configuration_check";

    public async Task<List<DiagnosticResult>> RunCheckAsync(IdPDiagnosticsContext ctx, CancellationToken ct)
    {
        var app = ctx.App;
        List<ErrorResult> errs = [];

        if (app.IdentityProviderRoleType != nameof(IdentityProviderRole.IDPServer))
            errs.Add(new("invalid_role", "Application is not configured as IDPServer"));

        if (string.IsNullOrWhiteSpace(app.OIDCIdPSubjectPepper))
            errs.Add(new("missing_configuration", "Application has no configured subject pepper"));

        if (string.IsNullOrWhiteSpace(app.OIDCCertificateUniqueID)
            || string.IsNullOrWhiteSpace(app.OIDCCertificatePassword)
            || (app.OIDCCertificateBlob == null || app.OIDCCertificateBlob.Length == 0))
            errs.Add(new("missing_configuration", "Application has no configured certificates for jwt_private_key"));

        if (app.OIDCClientConfiguration == null || app.OIDCClientConfiguration.Count == 0)
            errs.Add(new("no_clients_configured", "OIDCClientConfiguration is empty"));

        var result = errs.Count == 0
            ? new DiagnosticResult(DiagnosticId, IsSuccess: true, Result: "Status: OK")
            : new DiagnosticResult(DiagnosticId, IsSuccess: false, Result: "IdP configuration validation errors", Errors: errs);

        return [result];
    }
}