using MicroM.Core;
using MicroM.Diagnostics;
using System.Text.Json;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal class ClientEncryptionMetadataCheck : IDiagnosticCheck<ClientDiagnosticsContext>
{
    public string DiagnosticId => "oidc_client_encryption_metadata_check";

    // Supported/expected sets
    private static readonly HashSet<string> AllowedAlgs = new(StringComparer.OrdinalIgnoreCase)
    {
        "RSA-OAEP",          // RSA
        "ECDH-ES+A256KW",    // EC (key agreement + key wrap)
        "ECDH-ES"            // EC direct key agreement
    };

    private static readonly HashSet<string> DisallowedAlgs = new(StringComparer.OrdinalIgnoreCase)
    {
        "RSA-OAEP-256", // Not supported in .NET 8 libs (no RsaOaep256 constant)
        "dir",          // No symmetric dir for OIDC id_token here
        "A128KW","A192KW","A256KW",
        "A128GCMKW","A192GCMKW","A256GCMKW",
        "PBES2-HS256+A128KW","PBES2-HS384+A192KW","PBES2-HS512+A256KW"
    };

    private static readonly HashSet<string> AllowedEncs = new(StringComparer.OrdinalIgnoreCase)
    {
        "A256GCM", "A256CBC-HS512", "A192GCM", "A192CBC-HS384", "A128GCM", "A128CBC-HS256"
    };

    public async Task<List<DiagnosticResult>> RunCheckAsync(ClientDiagnosticsContext ctx, CancellationToken ct)
    {
        try
        {
            if (ctx.wellKnownDoc == null)
                return [new(DiagnosticId, Errors: [new("wellknown_missing", "Well-known document not loaded")])];

            var root = ctx.wellKnownDoc.RootElement;

            // 1) Validate subject_types_supported contains public,pairwise
            List<ErrorResult> subjectErrors = [];
            if (root.TryGetProperty("subject_types_supported", out var subj) && subj.ValueKind == JsonValueKind.Array)
            {
                var subjects = subj.EnumerateArray()
                    .Where(x => x.ValueKind == JsonValueKind.String)
                    .Select(x => x.GetString() ?? string.Empty)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (!subjects.Contains("public")) subjectErrors.Add(new("subject_public_missing", "subject_types_supported missing 'public'"));
                if (!subjects.Contains("pairwise")) subjectErrors.Add(new("subject_pairwise_missing", "subject_types_supported missing 'pairwise'"));
            }
            else
            {
                subjectErrors.Add(new("subject_types_missing", "subject_types_supported not present or not an array"));
            }

            // 2) Validate id_token_encryption_alg_values_supported
            List<ErrorResult> algErrors = [];
            HashSet<string> advertisedAlgs = new(StringComparer.OrdinalIgnoreCase);

            if (root.TryGetProperty("id_token_encryption_alg_values_supported", out var algs) && algs.ValueKind == JsonValueKind.Array)
            {
                foreach (var a in algs.EnumerateArray())
                {
                    if (a.ValueKind != JsonValueKind.String) continue;
                    var alg = a.GetString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(alg))
                    {
                        advertisedAlgs.Add(alg);
                        if (DisallowedAlgs.Contains(alg))
                            algErrors.Add(new("alg_disallowed", $"Disallowed alg advertised: {alg}"));

                        if (!AllowedAlgs.Contains(alg) && !DisallowedAlgs.Contains(alg))
                            algErrors.Add(new("alg_unknown", $"Unexpected alg advertised: {alg}"));
                    }
                }
            }
            else
            {
                algErrors.Add(new("algs_missing", "id_token_encryption_alg_values_supported not present or not an array"));
            }

            // Ensure at least one supported alg is present
            if (!advertisedAlgs.Overlaps(AllowedAlgs))
                algErrors.Add(new("algs_no_overlap", "No supported algs advertised (expect one of RSA-OAEP, ECDH-ES+A256KW, ECDH-ES)"));

            // 3) Validate id_token_encryption_enc_values_supported
            List<ErrorResult> encErrors = [];
            HashSet<string> advertisedEncs = new(StringComparer.OrdinalIgnoreCase);

            if (root.TryGetProperty("id_token_encryption_enc_values_supported", out var encs) && encs.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in encs.EnumerateArray())
                {
                    if (e.ValueKind != JsonValueKind.String) continue;
                    var enc = e.GetString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(enc))
                    {
                        advertisedEncs.Add(enc);
                        if (!AllowedEncs.Contains(enc))
                            encErrors.Add(new("enc_unknown", $"Unexpected enc advertised: {enc}"));
                    }
                }
            }
            else
            {
                encErrors.Add(new("encs_missing", "id_token_encryption_enc_values_supported not present or not an array"));
            }

            // Ensure A256GCM preference is advertised
            if (!advertisedEncs.Contains("A256GCM"))
                encErrors.Add(new("enc_preferred_missing", "A256GCM not advertised in id_token_encryption_enc_values_supported"));

            var allErrors = new List<ErrorResult>();
            allErrors.AddRange(subjectErrors);
            allErrors.AddRange(algErrors);
            allErrors.AddRange(encErrors);

            var summary = $"Encryption metadata validated. Algs=[{string.Join(", ", advertisedAlgs)}], Encs=[{string.Join(", ", advertisedEncs)}]";

            if (allErrors.Count == 0)
                return [new(DiagnosticId, IsSuccess: true, Result: summary)];

            return [new(DiagnosticId, Result: summary, Errors: [.. allErrors])];
        }
        catch (OperationCanceledException)
        {
            return [new(DiagnosticId, Errors: [new("cancelled", "Operation canceled")])];
        }
        catch (JsonException ex)
        {
            return [new(DiagnosticId, Errors: [new("json_error", ex.Message)])];
        }
        catch (Exception ex)
        {
            return [new(DiagnosticId, Errors: [new("unexpected_error", ex.Message)])];
        }
    }
}