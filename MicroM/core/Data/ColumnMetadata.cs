namespace MicroM.Data;

/// <summary>
/// Flags describing column behavior and usage.
/// </summary>
[Flags]
public enum ColumnFlags : byte
{
    /// <summary>No special behavior.</summary>
    None = 0,
    /// <summary>Column participates in retrieval operations.</summary>
    Get = 1,
    /// <summary>Column is used for insert operations.</summary>
    Insert = 2,
    /// <summary>Column is used for update operations.</summary>
    Update = 4,
    /// <summary>Column is used for delete operations.</summary>
    Delete = 8,
    /// <summary>Column is part of the primary key.</summary>
    PK = 16,
    /// <summary>Column is a foreign key.</summary>
    FK = 32,
    /// <summary>Column is auto-numbered.</summary>
    Autonum = 64,
    /// <summary>Column value is fake or placeholder.</summary>
    Fake = 128,
    /// <summary>Combination of all flags.</summary>
    All = 255
}
