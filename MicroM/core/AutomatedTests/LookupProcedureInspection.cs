using MicroM.Core;

namespace MicroM.AutomatedTests;

internal enum LookupProcedureBody
{
    Unknown,
    Empty,
    NonEmpty
}

internal static class LookupProcedureInspection
{
    internal static string GetQualifiedName(EntityBase entity)
    {
        string name = $"{entity.Def.Mneo}{nameof(DefaultProcedureNames._lookup)}";
        return entity.Def.Procs.TryGetValue(name, out var proc)
            ? proc.QualifiedName
            : string.IsNullOrEmpty(entity.Def.SchemaName) ? name : $"[{entity.Def.SchemaName}].{name}";
    }

    internal static async Task<(string ProcedureName, LookupProcedureBody Body)> Inspect(EntityBase entity, CancellationToken ct)
    {
        string name = GetQualifiedName(entity);
        string? definition;
        try
        {
            definition = await entity.Client.ExecuteSQLSingleColumn<string>($"SELECT OBJECT_DEFINITION(OBJECT_ID('{name}', N'P'))", ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"LOOKUP: Could not inspect deployed procedure '{name}' for entity '{entity.Def.Name}'.", ex);
        }

        return (name, ClassifyBody(definition));
    }

    internal static LookupProcedureBody ClassifyBody(string? definition)
    {
        if (string.IsNullOrWhiteSpace(definition) || !TryTokenize(definition, out var tokens))
        {
            return LookupProcedureBody.Unknown;
        }

        int index = 0;
        if (Take(tokens, ref index, "CREATE"))
        {
            if (Take(tokens, ref index, "OR") && !Take(tokens, ref index, "ALTER"))
            {
                return LookupProcedureBody.Unknown;
            }
        }
        else if (!Take(tokens, ref index, "ALTER"))
        {
            return LookupProcedureBody.Unknown;
        }

        if ((!Take(tokens, ref index, "PROC") && !Take(tokens, ref index, "PROCEDURE")) ||
            index >= tokens.Count || !IsIdentifier(tokens[index]))
        {
            return LookupProcedureBody.Unknown;
        }
        index++;
        while (Take(tokens, ref index, "."))
        {
            if (index >= tokens.Count || !IsIdentifier(tokens[index])) return LookupProcedureBody.Unknown;
            index++;
        }

        // Only unquoted tokens can be keywords. Skip execution-context and parameter-type AS.
        bool foundBody = false;
        for (; index < tokens.Count; index++)
        {
            if (!Is(tokens[index], "AS")) continue;
            if (index > 0 && (Is(tokens[index - 1], "EXECUTE") || tokens[index - 1].StartsWith('@'))) continue;
            index++;
            foundBody = true;
            break;
        }
        if (!foundBody) return LookupProcedureBody.Unknown;

        int blockDepth = 0;
        for (; index < tokens.Count; index++)
        {
            if (Is(tokens[index], ";")) continue;
            if (Is(tokens[index], "BEGIN")) blockDepth++;
            else if (Is(tokens[index], "END"))
            {
                if (--blockDepth < 0) return LookupProcedureBody.Unknown;
            }
            else return LookupProcedureBody.NonEmpty;
        }
        return blockDepth == 0 ? LookupProcedureBody.Empty : LookupProcedureBody.Unknown;
    }

    private static bool Is(string token, string keyword) => token.Equals(keyword, StringComparison.OrdinalIgnoreCase);

    private static bool Take(List<string> tokens, ref int index, string keyword)
    {
        if (index >= tokens.Count || !Is(tokens[index], keyword)) return false;
        index++;
        return true;
    }

    private static bool IsIdentifier(string token) =>
        token[0] == '[' || token[0] == '"' ||
        ((char.IsLetter(token[0]) || token[0] == '_' || token[0] == '#') && !Is(token, "AS"));

    // Keep strings and delimited identifiers intact so comment markers and keywords inside
    // them cannot disappear or become structural tokens. Do not concatenate across comments.
    private static bool TryTokenize(string sql, out List<string> tokens)
    {
        tokens = [];
        int index = 0;
        while (index < sql.Length)
        {
            char current = sql[index];
            if (char.IsWhiteSpace(current))
            {
                index++;
                continue;
            }
            if (current == '-' && index + 1 < sql.Length && sql[index + 1] == '-')
            {
                index += 2;
                while (index < sql.Length && sql[index] != '\r' && sql[index] != '\n') index++;
                continue;
            }
            if (current == '/' && index + 1 < sql.Length && sql[index + 1] == '*')
            {
                index += 2;
                int depth = 1;
                while (index < sql.Length && depth > 0)
                {
                    if (index + 1 < sql.Length && sql[index] == '/' && sql[index + 1] == '*')
                    {
                        depth++;
                        index += 2;
                    }
                    else if (index + 1 < sql.Length && sql[index] == '*' && sql[index + 1] == '/')
                    {
                        depth--;
                        index += 2;
                    }
                    else index++;
                }
                if (depth != 0) return false;
                continue;
            }

            int start = index++;
            if (current == '\'' || current == '"' || current == '[')
            {
                char closing = current == '[' ? ']' : current;
                bool closed = false;
                while (index < sql.Length)
                {
                    if (sql[index++] != closing) continue;
                    if (index < sql.Length && sql[index] == closing)
                    {
                        index++;
                        continue;
                    }
                    closed = true;
                    break;
                }
                if (!closed) return false;
            }
            else if (char.IsLetterOrDigit(current) || current is '_' or '@' or '#' or '$')
            {
                while (index < sql.Length && (char.IsLetterOrDigit(sql[index]) || sql[index] is '_' or '@' or '#' or '$')) index++;
            }
            tokens.Add(sql[start..index]);
        }
        return true;
    }
}
