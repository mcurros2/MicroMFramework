using MicroM.Data;
using MicroM.Extensions;
using System.Collections;
using System.Data.Common;

namespace MicroM.Excel;

public sealed class DataResultChannelDataReader : DbDataReader
{
    private readonly DataResultChannel _resultSet;
    private object?[]? _currentRow;
    private bool _isClosed;

    public DataResultChannelDataReader(DataResultChannel resultSet)
    {
        _resultSet = resultSet ?? throw new ArgumentNullException(nameof(resultSet));
    }

    public override int FieldCount => _resultSet.Header.Length;
    public override int Depth => 0;
    public override int RecordsAffected => -1;
    public override bool IsClosed => _isClosed;

    public override object this[int ordinal] => GetValue(ordinal);
    public override object this[string name] => GetValue(GetOrdinal(name));

    public override string GetName(int ordinal)
    {
        ValidateOrdinal(ordinal);
        return _resultSet.Header[ordinal] ?? $"Column{ordinal}";
    }

    public override string GetDataTypeName(int ordinal)
    {
        ValidateOrdinal(ordinal);
        return _resultSet.typeInfo[ordinal];
    }

    public override Type GetFieldType(int ordinal)
    {
        ValidateOrdinal(ordinal);
        return _resultSet.GetHeaderType(ordinal);
    }

    public override int GetOrdinal(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        for (int i = 0; i < _resultSet.Header.Length; i++)
        {
            if (string.Equals(_resultSet.Header[i], name, StringComparison.Ordinal))
                return i;
        }

        for (int i = 0; i < _resultSet.Header.Length; i++)
        {
            if (string.Equals(_resultSet.Header[i], name, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        throw new IndexOutOfRangeException($"Column '{name}' not found.");
    }

    public override object GetValue(int ordinal)
    {
        EnsureNotClosed();
        ValidateCurrentRow();
        ValidateOrdinal(ordinal);

        return _currentRow![ordinal] ?? DBNull.Value;
    }

    public override int GetValues(object[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        int length = Math.Min(values.Length, FieldCount);
        for (int i = 0; i < length; i++)
        {
            values[i] = GetValue(i);
        }

        return length;
    }

    public override bool IsDBNull(int ordinal) => GetValue(ordinal) is DBNull;

    public override bool HasRows
    {
        get
        {
            EnsureNotClosed();

            if (_currentRow is not null)
                return true;

            return !_resultSet.records.Reader.Completion.IsCompleted;
        }
    }

    public override bool Read()
    {
        EnsureNotClosed();

        while (_resultSet.records.Reader.WaitToReadAsync().AsTask().GetAwaiter().GetResult())
        {
            if (_resultSet.records.Reader.TryRead(out var record))
            {
                ValidateRecord(record);
                _currentRow = record;
                return true;
            }
        }

        _currentRow = null;
        return false;
    }

    public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        EnsureNotClosed();

        while (await _resultSet.records.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_resultSet.records.Reader.TryRead(out var record))
            {
                ValidateRecord(record);
                _currentRow = record;
                return true;
            }
        }

        _currentRow = null;
        return false;
    }

    public override bool NextResult() => false;

    public override void Close()
    {
        _isClosed = true;
        _currentRow = null;
    }

    public override IEnumerator GetEnumerator()
    {
        while (Read())
        {
            yield return this;
        }
    }

    public override bool GetBoolean(int ordinal) => GetFieldValue<bool>(ordinal);
    public override byte GetByte(int ordinal) => GetFieldValue<byte>(ordinal);
    public override char GetChar(int ordinal) => GetFieldValue<char>(ordinal);
    public override short GetInt16(int ordinal) => GetFieldValue<short>(ordinal);
    public override int GetInt32(int ordinal) => GetFieldValue<int>(ordinal);
    public override long GetInt64(int ordinal) => GetFieldValue<long>(ordinal);
    public override float GetFloat(int ordinal) => GetFieldValue<float>(ordinal);
    public override double GetDouble(int ordinal) => GetFieldValue<double>(ordinal);
    public override decimal GetDecimal(int ordinal) => GetFieldValue<decimal>(ordinal);
    public override Guid GetGuid(int ordinal) => GetFieldValue<Guid>(ordinal);

    public override string GetString(int ordinal)
    {
        var value = GetValue(ordinal);

        if (value is DBNull)
            throw new InvalidCastException("Column value is DBNull.");

        return value.ToString() ?? string.Empty;
    }

    public override DateTime GetDateTime(int ordinal)
    {
        var value = GetValue(ordinal);

        return value switch
        {
            DateTime dt => dt,
            DateOnly d => d.ToDateTime(TimeOnly.MinValue),
            DBNull => throw new InvalidCastException("Column value is DBNull."),
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to DateTime.")
        };
    }

    public override T GetFieldValue<T>(int ordinal)
    {
        var value = GetValue(ordinal);

        if (value is DBNull)
        {
            if (default(T) is null)
                return default!;

            throw new InvalidCastException($"Cannot convert DBNull to {typeof(T).Name}.");
        }

        if (typeof(T) == typeof(DateTime) && value is DateOnly d)
            return (T)(object)d.ToDateTime(TimeOnly.MinValue);

        if (value is T t)
            return t;

        return (T)Convert.ChangeType(value, typeof(T));
    }

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
    {
        var value = GetValue(ordinal);

        if (value is DBNull)
            return 0;

        if (value is not byte[] data)
            throw new InvalidCastException($"Column {ordinal} is not a byte array.");

        if (dataOffset < 0 || dataOffset > data.Length)
            throw new ArgumentOutOfRangeException(nameof(dataOffset));

        int available = data.Length - (int)dataOffset;

        if (buffer is null)
            return available;

        if (bufferOffset < 0 || bufferOffset > buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(bufferOffset));

        if (length < 0 || bufferOffset + length > buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(length));

        int toCopy = Math.Min(length, available);
        Array.Copy(data, (int)dataOffset, buffer, bufferOffset, toCopy);
        return toCopy;
    }

    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
    {
        var value = GetValue(ordinal);

        if (value is DBNull)
            return 0;

        char[] data = value switch
        {
            string s => s.ToCharArray(),
            char[] c => c,
            _ => throw new InvalidCastException($"Column {ordinal} is not a string or char array.")
        };

        if (dataOffset < 0 || dataOffset > data.Length)
            throw new ArgumentOutOfRangeException(nameof(dataOffset));

        int available = data.Length - (int)dataOffset;

        if (buffer is null)
            return available;

        if (bufferOffset < 0 || bufferOffset > buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(bufferOffset));

        if (length < 0 || bufferOffset + length > buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(length));

        int toCopy = Math.Min(length, available);
        Array.Copy(data, (int)dataOffset, buffer, bufferOffset, toCopy);
        return toCopy;
    }

    private void EnsureNotClosed()
    {
        if (_isClosed)
            throw new InvalidOperationException("The reader is closed.");
    }

    private void ValidateCurrentRow()
    {
        if (_currentRow is null)
            throw new InvalidOperationException("No current row. Call Read or ReadAsync first.");
    }

    private void ValidateOrdinal(int ordinal)
    {
        if ((uint)ordinal >= (uint)FieldCount)
            throw new IndexOutOfRangeException($"Invalid ordinal {ordinal}.");
    }

    private void ValidateRecord(object?[] record)
    {
        if (record.Length != FieldCount)
        {
            throw new InvalidOperationException(
                $"Record length {record.Length} does not match FieldCount {FieldCount}.");
        }
    }

}