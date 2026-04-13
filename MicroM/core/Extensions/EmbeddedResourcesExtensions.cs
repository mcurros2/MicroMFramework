using MicroM.Configuration;
using MicroM.Core;
using MicroM.Database;
using System.Reflection;
using System.Text;

namespace MicroM.Extensions;

public static class EmbeddedResourcesExtensions
{
    private record SQLReplacement(string StringToFind, string ReplaceWith);

    private static List<SQLReplacement> BuildReplacements()
    {
        return [
            new SQLReplacement("[dbo].", $"[{DataDefaults.DataDictionarySchema ?? "dbo"}]."),
        ];
    }

    private static string ReplaceEntityReferences(this string sqlText, List<SQLReplacement> replacements)
    {
        if (string.IsNullOrEmpty(sqlText)) return sqlText;

        if (replacements.Count == 0) return sqlText;

        if (replacements.Count == 1 && replacements[0].StringToFind.Equals(replacements[0].ReplaceWith, StringComparison.OrdinalIgnoreCase)) return sqlText;

        StringBuilder result = new(sqlText);

        foreach (var replacement in replacements)
        {
            result.Replace(replacement.StringToFind, replacement.ReplaceWith);
        }

        return result.ToString();
    }

    public async static Task<List<string>> GetAllCustomProcs<T>(this T assembly_class, string? mneo, CancellationToken ct, bool replace_dd_schema = false) where T : class
    {
        var replacements = BuildReplacements();

        var assembly = typeof(T).Assembly;
        List<string> ret = [];
        foreach (string name in assembly.GetManifestResourceNames())
        {
            if (name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) && ((name.Contains($".{mneo}_") || name.StartsWith($"{mneo}_", StringComparison.OrdinalIgnoreCase)) || mneo == null))
            {
                using var manifest = assembly.GetManifestResourceStream(name);
                if (manifest != null)
                {
                    using StreamReader reader = new(manifest);
                    var sqlText = await reader.ReadToEndAsync(ct);
                    ret.Add(replace_dd_schema ? sqlText.ReplaceEntityReferences(replacements) : sqlText);
                    reader.Close();
                }
            }
        }
        return ret;
    }

    public async static Task<List<string>> GetAssemblyCustomProcs(this Assembly assembly, string? mneo, string? starts_with, CancellationToken ct, bool replace_dd_schema = false)
    {
        var replacements = BuildReplacements();

        List<string> ret = [];
        foreach (string name in assembly.GetManifestResourceNames())
        {
            if (!name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)) continue;

            if (starts_with != null && !name.StartsWith(starts_with, StringComparison.OrdinalIgnoreCase) && !name.Contains($".{starts_with}", StringComparison.OrdinalIgnoreCase)) continue;

            if (mneo != null && !name.Contains($".{mneo}_") && !name.StartsWith($"{mneo}_", StringComparison.OrdinalIgnoreCase)) continue;

            using var manifest = assembly.GetManifestResourceStream(name);
            if (manifest != null)
            {
                using StreamReader reader = new(manifest);
                var sqlText = await reader.ReadToEndAsync(ct);
                ret.Add(replace_dd_schema ? sqlText.ReplaceEntityReferences(replacements) : sqlText);
                reader.Close();
            }
        }
        return ret;
    }

    public async static Task<CustomOrderedDictionary<CustomScript>> GetAllClassifiedCustomProcs(this Assembly assembly, CancellationToken ct, bool replace_dd_schema = false)
    {
        CustomOrderedDictionary<CustomScript> ret = new();

        var replacements = BuildReplacements();

        foreach (string name in assembly.GetManifestResourceNames())
        {
            ct.ThrowIfCancellationRequested();
            if (!name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)) continue;

            using var manifest = assembly.GetManifestResourceStream(name);
            if (manifest != null)
            {
                using StreamReader reader = new(manifest);
                string sqlText = await reader.ReadToEndAsync(ct);
                reader.Close();

                string custom_sql = replace_dd_schema ? sqlText.ReplaceEntityReferences(replacements) : sqlText;

                foreach (var custom_proc in DatabaseSchemaCustomScripts.ClassifyCustomSQLScript(custom_sql))
                {
                    ret.Add(custom_proc.ProcName == null || !custom_proc.ProcType.IsIn(SQLScriptType.Procedure, SQLScriptType.Function) ? $"{Guid.NewGuid()}" : custom_proc.ProcName, custom_proc);
                }
            }
        }

        return ret;
    }

}