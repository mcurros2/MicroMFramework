using MicroM.Extensions;
using System.Data;

namespace MicroM.Data;

/// <summary>
/// Encapsulates SQL Server type metadata for a column.
/// </summary>
public sealed class SQLServerMetadata
{
    /// <summary>The SQL Server data type.</summary>
    public readonly SqlDbType SQLType;

    /// <summary>The size of the type.</summary>
    public readonly int Size;

    /// <summary>Numeric precision.</summary>
    public readonly byte Precision;

    /// <summary>Numeric scale.</summary>
    public readonly byte Scale;

    /// <summary>Indicates whether the parameter is an output.</summary>
    public readonly bool Output;

    /// <summary>Indicates whether null values are allowed.</summary>
    public readonly bool Nullable;

    /// <summary>Indicates whether the value is encrypted.</summary>
    public readonly bool Encrypted;

    /// <summary>Indicates whether the value represents an array.</summary>
    public readonly bool IsArray;

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLServerMetadata"/> class.
    /// </summary>
    public SQLServerMetadata(SqlDbType sql_type, int size = 0, byte precision = 0, byte scale = 0,
        bool output = false, bool nullable = false, bool encrypted = false, bool isArray = false)
    {
        if (sql_type.IsIn(SqlDbType.Char, SqlDbType.NChar) && size == 0)
            throw new ArgumentOutOfRangeException(nameof(size), size, $"You must specify a size greater than 0 for {sql_type}");

        SQLType = sql_type;
        Size = size;
        Precision = precision;
        Scale = scale;
        Output = output;
        Nullable = nullable;
        Encrypted = encrypted;
        IsArray = isArray;
    }

}
