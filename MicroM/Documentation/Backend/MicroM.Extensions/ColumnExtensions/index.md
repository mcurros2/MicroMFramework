# Class: MicroM.Extensions.ColumnExtensions
## Overview
Extensions for working with `ColumnBase` collections and values.

**Inheritance**
object -> ColumnExtensions

**Implements**
None

## Example Usage
```csharp
var encrypted = columns.GetWithFlags(ColumnFlags.PK).Values;
```
## Methods
| Method | Description |
|:------------|:-------------|
| GetWithFlags(IReadonlyOrderedDictionary<ColumnBase> cols, ColumnFlags flags, ColumnFlags exclude_flags = ColumnFlags.Fake, params string[] exclude_names) | Retrieves columns matching the specified flags. |
| SetColumnValuesByName<T>(T cols, IReadonlyOrderedDictionary<ColumnBase> values, params string[] names_to_copy) | Copies values by column name into the target collection. |
| CopyColumnValuesByName(IReadonlyOrderedDictionary<ColumnBase> cols, IReadonlyOrderedDictionary<ColumnBase> source) | Copies column values from a source collection. |
| GetParmsWithFlags(IReadonlyOrderedDictionary<ColumnBase> cols, ColumnFlags flags) | Returns columns with matching flags. |
| AddExisting(CustomOrderedDictionary<ColumnBase> collection, ColumnBase col) | Adds an existing column to the collection. |
| ContainsAllKeys(IReadonlyOrderedDictionary<ColumnBase> source, IReadonlyOrderedDictionary<ColumnBase> cols) | Determines whether all keys are contained. |
| ContainsAllKeys(IReadonlyOrderedDictionary<ColumnBase> source, params string[] keys) | Checks if all provided keys exist in the collection. |
| EncryptColumnData(IEnumerable<ColumnBase> cols, IMicroMEncryption encryptor) | Encrypts string column values using the provided encryptor. |
| DecryptColumnData(IEnumerable<ColumnBase> cols, IMicroMEncryption encryptor) | Decrypts string column values using the provided encryptor. |
| MapColumnData<T>(IReadonlyOrderedDictionary<ColumnBase> cols) where T : class, new() | Maps column data to a new instance of `T`. |
| AddCol<T>(CustomOrderedDictionary<ColumnBase> collection, string name, SqlDbType? sql_type, int size = 0, byte precision = 0, byte scale = 0, T value = default!) | Adds a new column definition. |
| AddPK<T>(CustomOrderedDictionary<ColumnBase> collection, string name, SqlDbType? sql_type = SqlDbType.Char, int size = 20, byte precision = 0, byte scale = 0, T value = default!) | Adds a primary key column definition. |
| AddFK<T>(CustomOrderedDictionary<ColumnBase> collection, string name, SqlDbType? sql_type = SqlDbType.Char, int size = 20, byte precision = 0, byte scale = 0, T value = default!) | Adds a foreign key column definition. |
| CreateSQLParameter(ColumnBase sql_col) | Creates a `SqlParameter` from a column. |
| AsSqlParameters(IEnumerable<ColumnBase> sql_cols) | Converts columns to an array of `SqlParameter`. |
| StripColumnPrefix(string column_name) | Removes table prefix from a column name. |
| AsViewItemParm(ColumnBase column, int column_mapping = -1, string compound_group = "", int compound_position = -1, bool compound_key = false, bool browsing_key = false) | Converts a column to a `ViewParm`. |
| ToDictionary(IReadonlyOrderedDictionary<ColumnBase> cols, HashSet<string>? exclude_colnames = null) | Converts columns to a dictionary of values. |
| ToColumnsDictionary(IReadonlyOrderedDictionary<ColumnBase> cols, HashSet<string>? exclude_colnames = null) | Creates a dictionary keyed by column name. |
| TryConvertFromString(ColumnBase col, string? value_to_convert, out object? converted_value) | Attempts to convert a string into the column's data type. |
| TryConvertFromJsonElement<T>(JsonElement source, out T result) | Attempts to convert a JSON element to type `T`. |
| TryConvertFromJsonElement(JsonElement element, ColumnBase col, out object? converted_value) | Attempts to convert a JSON element into column value. |

## Remarks
None.

## See Also
-
