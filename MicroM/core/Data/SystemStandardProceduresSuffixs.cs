using MicroM.Database;

namespace MicroM.Data;

/// <summary>
/// Contains standard suffixes used for system stored procedures.
/// </summary>
public static class SystemStandardProceduresSuffixs
{
    public const string _update = "_update";
    public const string _drop = "_drop";
    public const string _get = "_get";
    public const string _brwStandard = "_brwStandard";
    public const string _lookup = "_lookup";
    public const string _iupdate = "_iupdate";
    public const string _idrop = "_idrop";

    /// <summary>
    /// Determines the standard procedure type based on its name.
    /// </summary>
    public static SQLProcStandardType GetProcStandardType(string? proc_name)
    {
        if (string.IsNullOrWhiteSpace(proc_name)) return SQLProcStandardType.Unknown;

        if (proc_name.EndsWith(_update, StringComparison.OrdinalIgnoreCase))
            return SQLProcStandardType.Update;

        if (proc_name.EndsWith(_drop, StringComparison.OrdinalIgnoreCase))
            return SQLProcStandardType.Drop;

        if (proc_name.EndsWith(_get, StringComparison.OrdinalIgnoreCase))
            return SQLProcStandardType.Get;

        if (proc_name.EndsWith(_brwStandard, StringComparison.OrdinalIgnoreCase))
            return SQLProcStandardType.StandardView;

        if (proc_name.EndsWith(_lookup, StringComparison.OrdinalIgnoreCase))
            return SQLProcStandardType.Lookup;

        if (proc_name.EndsWith(_iupdate, StringComparison.OrdinalIgnoreCase))
            return SQLProcStandardType.IUpdate;

        if (proc_name.EndsWith(_idrop, StringComparison.OrdinalIgnoreCase))
            return SQLProcStandardType.IDrop;

        return SQLProcStandardType.Unknown;
    }
}