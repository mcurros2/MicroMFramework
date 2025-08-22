using MicroM.Core;
using MicroM.Extensions;
using System.Text.RegularExpressions;
using static MicroM.Data.SystemStandardProceduresSuffixs;

namespace MicroM.Database;

/// <summary>
/// Helpers to parse and classify custom SQL scripts embedded in assemblies.
/// </summary>
public static partial class DatabaseSchemaCustomScripts
{
    /// <summary>
    /// Extracts basic information from a SQL script and classifies it.
    /// </summary>
    /// <param name="sql_text">SQL script text.</param>
    /// <returns>The classified script or <c>null</c> if not recognized.</returns>
    public static CustomScript? ExtractCustomScript(string sql_text)
    {
        if (string.IsNullOrWhiteSpace(sql_text))
            return null;

        // Delete block comments /* ... */
        while (true)
        {
            int start = sql_text.IndexOf("/*", StringComparison.OrdinalIgnoreCase);
            if (start == -1) break;
            int end = sql_text.IndexOf("*/", start + 2, StringComparison.OrdinalIgnoreCase);
            if (end == -1) break;
            sql_text = sql_text.Remove(start, end - start + 2);
        }

        var lines = sql_text.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            var codePart = line.Split(["--"], StringSplitOptions.None)[0].Trim();

            string[] tokens = codePart.Split([' ', '\t', '('], StringSplitOptions.RemoveEmptyEntries);

            // should be create proc xxxx at least
            if (tokens.Length < 3)
                continue;

            string? proc_name;
            SQLScriptType proc_type = SQLScriptType.Unknown;
            if (tokens.Length >= 5 &&
                tokens[0].Equals("create", StringComparison.OrdinalIgnoreCase) &&
                tokens[1].Equals("or", StringComparison.OrdinalIgnoreCase) &&
                tokens[2].Equals("alter", StringComparison.OrdinalIgnoreCase)
                )
            {
                proc_name = tokens[4];
                if (tokens[3].Equals("proc", StringComparison.OrdinalIgnoreCase) || tokens[3].Equals("procedure", StringComparison.OrdinalIgnoreCase))
                {
                    proc_type = SQLScriptType.Procedure;
                }
                else if (tokens[3].Equals("function", StringComparison.OrdinalIgnoreCase))
                {
                    proc_type = SQLScriptType.Function;
                }
                else if (tokens[1].Equals("trigger", StringComparison.OrdinalIgnoreCase))
                {
                    proc_type = SQLScriptType.Trigger;
                }
                else if (tokens[1].Equals("view", StringComparison.OrdinalIgnoreCase))
                {
                    proc_type = SQLScriptType.View;
                }
                else
                {
                    proc_type = SQLScriptType.Unknown;
                }

                // remove schema name if present
                if (proc_name.Contains('.'))
                {
                    proc_name = proc_name.Split('.')[1];
                }

                proc_name = proc_name.Trim('[', ']');
                // extract mneo from proc name. Should be the first part of the name [mneo]_[proc_name]
                string? mneo = null;
                if (proc_name.Contains('_'))
                {
                    mneo = proc_name.Split('_')[0];
                }

                return new(proc_name, mneo, proc_type, GetProcStandardType(proc_name), sql_text);
            }
            else if (
                tokens[0].Equals("create", StringComparison.OrdinalIgnoreCase) ||
                tokens[0].Equals("alter", StringComparison.OrdinalIgnoreCase)
                )
            {
                proc_name = tokens[2];

                if (tokens[1].Equals("proc", StringComparison.OrdinalIgnoreCase) || tokens[1].Equals("procedure", StringComparison.OrdinalIgnoreCase))
                {
                    proc_type = SQLScriptType.Procedure;
                }
                else if (tokens[1].Equals("function", StringComparison.OrdinalIgnoreCase))
                {
                    proc_type = SQLScriptType.Function;
                }
                else if (tokens[1].Equals("sequence", StringComparison.OrdinalIgnoreCase))
                {
                    proc_type = SQLScriptType.Sequence;
                }
                else if (tokens[1].Equals("type", StringComparison.OrdinalIgnoreCase))
                {
                    proc_type = SQLScriptType.Type;
                }
                else if (tokens[1].Equals("view", StringComparison.OrdinalIgnoreCase))
                {
                    proc_type = SQLScriptType.View;
                }
                else if (tokens[1].Equals("trigger", StringComparison.OrdinalIgnoreCase))
                {
                    proc_type = SQLScriptType.Trigger;
                }
                else if (tokens[1].Equals("table", StringComparison.OrdinalIgnoreCase))
                {
                    proc_type = SQLScriptType.Table;
                }
                else
                {
                    proc_type = SQLScriptType.Unknown;
                }

                // remove schema name if present
                if (proc_name.Contains('.'))
                {
                    proc_name = proc_name.Split('.')[1];
                }
                proc_name = proc_name.Trim('[', ']');
                string? mneo = null;
                if (proc_name.Contains('_'))
                {
                    mneo = proc_name.Split('_')[0];
                }
                return new(proc_name, mneo, proc_type, GetProcStandardType(proc_name), sql_text);
            }

        }

        return new(null, null, SQLScriptType.Unknown, SQLProcStandardType.Unknown, sql_text);
    }

    [GeneratedRegex(@"^\s*GO\s*(?:\r?\n|$)", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex SplitSQLScriptsRegex();

    /// <summary>
    /// Splits and classifies a SQL script into individual custom scripts.
    /// </summary>
    /// <param name="sql_script">Combined SQL script.</param>
    /// <returns>Enumerated custom scripts.</returns>
    public static IEnumerable<CustomScript> ClassifyCustomSQLScript(string sql_script)
    {

        var single_proc_scripts = SplitSQLScriptsRegex().Split(sql_script).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

        foreach (var single_script in single_proc_scripts)
        {
            var custom_proc = ExtractCustomScript(single_script);

            if (custom_proc != null)
            {
                yield return custom_proc;
            }
        }
    }

    /// <summary>
    /// Classifies a list of SQL scripts into custom script objects.
    /// </summary>
    /// <param name="sql_scripts">Collection of SQL scripts.</param>
    /// <returns>Dictionary of classified custom scripts.</returns>
    public static CustomOrderedDictionary<CustomScript> ClassifyCustomSQLScripts(List<string> sql_scripts)
    {
        CustomOrderedDictionary<CustomScript> procs = new();

        foreach (string script in sql_scripts)
        {
            foreach (var custom_proc in ClassifyCustomSQLScript(script))
            {
                procs.Add(custom_proc.ProcName == null || !custom_proc.ProcType.IsIn(SQLScriptType.Procedure, SQLScriptType.Function) ? $"{Guid.NewGuid()}" : custom_proc.ProcName, custom_proc);
            }
        }

        return procs;
    }


}
