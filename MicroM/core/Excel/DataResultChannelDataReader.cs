using MicroM.Data;
using MicroM.Extensions;
using System.Collections;
using System.Data.Common;

namespace MicroM.Excel;


public class DataResultChannelDataReader : DbDataReader
{
    private readonly DataResultChannel _resultSet;
    private object?[]? _currentRow;

    public DataResultChannelDataReader(DataResultChannel resultSet)
    {
        _resultSet = resultSet;
    }

    public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        while (await _resultSet.records.Reader.WaitToReadAsync(cancellationToken))
        {
            if (_resultSet.records.Reader.TryRead(out var record))
            {
                _currentRow = record;
                return true;
            }
        }
        return false;
    }

    public override bool HasRows => true;
    public override bool IsClosed => false;
    public override int RecordsAffected => -1;
    public override int Depth => 0;
    public override int FieldCount => _resultSet.Header.Length;

    public override string GetName(int ordinal) => _resultSet.Header[ordinal];

    public override object GetValue(int ordinal) => _currentRow?[ordinal] ?? DBNull.Value;

    public override Type GetFieldType(int ordinal) => _resultSet.GetHeaderType(ordinal);

    public override int GetOrdinal(string name) => Array.IndexOf(_resultSet.Header, name);

    public override bool IsDBNull(int ordinal) => GetValue(ordinal) == DBNull.Value;

    public override object this[string name] => throw new NotImplementedException();

    public override object this[int ordinal] => throw new NotImplementedException();

    public override bool NextResult() => false;
    public override void Close() { }
    public override bool Read()
    {
        throw new NotSupportedException("Use ReadAsync instead.");
    }

    public override bool GetBoolean(int ordinal) => (bool)GetValue(ordinal);
    public override byte GetByte(int ordinal) => (byte)GetValue(ordinal);
    public override char GetChar(int ordinal) => (char)GetValue(ordinal);
    public override DateTime GetDateTime(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value is DateOnly dateOnly)
        {
            return dateOnly.ToDateTime(TimeOnly.MinValue);
        }

        return (DateTime)value;
    }
    public override decimal GetDecimal(int ordinal) => (decimal)GetValue(ordinal);
    public override double GetDouble(int ordinal) => (double)GetValue(ordinal);
    public override float GetFloat(int ordinal) => (float)GetValue(ordinal);
    public override Guid GetGuid(int ordinal) => (Guid)GetValue(ordinal);
    public override short GetInt16(int ordinal) => (short)GetValue(ordinal);
    public override int GetInt32(int ordinal) => (int)GetValue(ordinal);
    public override long GetInt64(int ordinal) => (long)GetValue(ordinal);
    public override string GetString(int ordinal) => GetValue(ordinal).ToString() ?? string.Empty;

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();

    public override int GetValues(object[] values)
    {
        int length = Math.Min(values.Length, FieldCount);
        for (int i = 0; i < length; i++) values[i] = GetValue(i);
        return length;
    }

    public override T GetFieldValue<T>(int ordinal)
    {
        var value = GetValue(ordinal);

        if (typeof(T) == typeof(DateTime) && value is DateOnly d)
        {
            return (T)(object)d.ToDateTime(TimeOnly.MinValue);
        }

        return (T)value!;
    }

    public override string GetDataTypeName(int ordinal) => _resultSet.typeInfo[ordinal];

    public override IEnumerator GetEnumerator() => throw new NotImplementedException();
}