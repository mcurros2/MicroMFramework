using MicroM.Extensions;
using System.Data;

namespace MicroM.Data
{
    public sealed class SQLServerMetadata
    {
        public readonly SqlDbType SQLType;
        public readonly int Size;
        public readonly byte Precision;
        public readonly byte Scale;
        public readonly bool Output;
        public readonly bool Nullable;
        public readonly bool Encrypted;
        public readonly bool IsArray;

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

}
