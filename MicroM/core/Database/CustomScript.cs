namespace MicroM.Database;

public enum SQLScriptType
{
    Procedure,
    Function,
    Type,
    Sequence,
    View,
    Trigger,
    Table,
    Unknown
}

public enum SQLProcStandardType
{
    Update,
    Get,
    Lookup,
    StandardView,
    Drop,
    IDrop,
    IUpdate,
    Unknown
}

public record CustomScript(string? ProcName, string? mneo, SQLScriptType ProcType, SQLProcStandardType StandardType, string SQLText);

