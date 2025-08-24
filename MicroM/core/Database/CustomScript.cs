namespace MicroM.Database;

/// <summary>
/// Types of SQL scripts supported by MicroM.
/// </summary>
public enum SQLScriptType
{
    /// <summary>Stored procedure.</summary>
    Procedure,
    /// <summary>Function definition.</summary>
    Function,
    /// <summary>User defined type.</summary>
    Type,
    /// <summary>Sequence definition.</summary>
    Sequence,
    /// <summary>View definition.</summary>
    View,
    /// <summary>Trigger definition.</summary>
    Trigger,
    /// <summary>Table creation script.</summary>
    Table,
    /// <summary>Unknown script type.</summary>
    Unknown
}

/// <summary>
/// Standard categories for generated procedures.
/// </summary>
public enum SQLProcStandardType
{
    /// <summary>Update procedure.</summary>
    Update,
    /// <summary>Get procedure.</summary>
    Get,
    /// <summary>Lookup procedure.</summary>
    Lookup,
    /// <summary>Standard view.</summary>
    StandardView,
    /// <summary>Drop procedure.</summary>
    Drop,
    /// <summary>Installation drop script.</summary>
    IDrop,
    /// <summary>Installation update script.</summary>
    IUpdate,
    /// <summary>Unknown procedure type.</summary>
    Unknown
}

/// <summary>
/// Represents a custom SQL script and its classification.
/// </summary>
/// <param name="ProcName">Name of the procedure or object.</param>
/// <param name="mneo">Associated mnemonic.</param>
/// <param name="ProcType">Type of script.</param>
/// <param name="StandardType">Standard procedure category.</param>
/// <param name="SQLText">SQL text of the script.</param>
public record CustomScript(string? ProcName, string? mneo, SQLScriptType ProcType, SQLProcStandardType StandardType, string SQLText);

