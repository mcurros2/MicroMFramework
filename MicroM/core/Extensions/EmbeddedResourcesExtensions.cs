using MicroM.Core;
using MicroM.Database;
using System.Reflection;
using System.Text;

namespace MicroM.Extensions;

public static class EmbeddedResourcesExtensions
{
    private record SQLReplacement(string StringToFind, string ReplaceWith);

    private static List<SQLReplacement> BuildReplacements(string? schema_name)
    {
        return [
            new SQLReplacement("[dbo].", $"[{schema_name ?? "dbo"}]."),
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

    private static string ApplyCustomSQLTransform(string sql_text, Func<string, string?>? custom_sql_transform)
    {
        return custom_sql_transform?.Invoke(sql_text) ?? sql_text;
    }

    public async static Task<List<string>> GetAllCustomProcs<T>(this T entity, string? mneo, CancellationToken ct, Func<string, string?>? custom_sql_transform = null) where T : EntityBase
    {
        var replacements = BuildReplacements(entity.Def.SchemaName);

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
                    var schema_sql = entity.Def.SchemaName != null ? sqlText.ReplaceEntityReferences(replacements) : sqlText;
                    ret.Add(ApplyCustomSQLTransform(schema_sql, custom_sql_transform));
                    reader.Close();
                }
            }
        }
        return ret;
    }

    public async static Task<List<string>> GetAssemblyCustomProcs(this Assembly assembly, string? mneo, string? starts_with, CancellationToken ct, string? schema_name = null, Func<string, string?>? custom_sql_transform = null)
    {
        var replacements = BuildReplacements(schema_name);

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
                var schema_sql = schema_name != null ? sqlText.ReplaceEntityReferences(replacements) : sqlText;
                ret.Add(ApplyCustomSQLTransform(schema_sql, custom_sql_transform));
                reader.Close();
            }
        }
        return ret;
    }

    public async static Task<CustomOrderedDictionary<CustomScript>> GetAllClassifiedCustomSQLScripts(this Assembly assembly, CancellationToken ct, string? schema_name = null, Func<string, string?>? custom_sql_transform = null)
    {
        CustomOrderedDictionary<CustomScript> ret = new();

        var replacements = BuildReplacements(schema_name);
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

                string schema_sql = schema_name != null ? sqlText.ReplaceEntityReferences(replacements) : sqlText;
                string custom_sql = ApplyCustomSQLTransform(schema_sql, custom_sql_transform);

                foreach (var custom_proc in DatabaseSchemaCustomScripts.ClassifyCustomSQLScript(custom_sql))
                {
                    ret.Add(custom_proc.ProcName == null || !custom_proc.ProcType.IsIn(SQLScriptType.Procedure, SQLScriptType.Function) ? $"{Guid.NewGuid()}" : custom_proc.ProcName, custom_proc);
                }
            }
        }

        return ret;
    }

}
