using MicroM.Extensions;
using System.Data;
using System.Text.Json;

namespace MicroM.Data
{
    public abstract class ColumnBase
    {
        public readonly Type SystemType = null!;

        public ColumnFlags ColumnMetadata { get; init; }

        public SQLServerMetadata SQLMetadata { get; private set; }

        private string _Name = null!;
        public string Name
        {
            get => _Name;
            internal set
            {
                if (string.IsNullOrEmpty(_Name))
                {
                    _Name = value;
                }
                else throw new ArgumentException($"The property {nameof(Name)} can only be modified if the value is null.");
            }
        }

        private object? GetValueType(object? value, string property_name)
        {
            object? result = null;
            if (value != null)
            {
                Type value_type;

                if (value is JsonElement element)
                {
                    element.TryConvertFromJsonElement(this, out result);
                }
                else
                {
                    result = value;
                }
                if (result != null)
                {
                    value_type = result.GetType();
                    if (!SQLMetadata.SQLType.IsTypeAccepted(value_type))
                        throw new ArgumentOutOfRangeException(property_name, value, $"The type {value_type} is not valid for SQL Server Type {SQLMetadata.SQLType}. Column: {this.Name}");
                }

            }
            return result;
        }

        private object? _value;
        public object? ValueObject
        {
            get => _value;
            set
            {
                if (SQLMetadata.Nullable && value is DBNull)
                {
                    _value = null;
                }
                else
                {
                    _value = GetValueType(value, nameof(ValueObject));
                }
            }
        }

        private string _SQLParameterName = null!;
        public string SQLParameterName
        {
            get
            {
                if (string.IsNullOrEmpty(_SQLParameterName) && !string.IsNullOrEmpty(_Name))
                {
                    _SQLParameterName = $"{_Name.StripColumnPrefix()}";
                }
                return _SQLParameterName;
            }
        }

        public string? RelatedCategoryID { get; init; }
        public string? RelatedStatusID { get; init; }

        public string? OverrideWith { get; init; }

        public ColumnBase(
            Type system_type
            , string name
            , object? value = default
            , SqlDbType? sql_type = null
            , int size = 0
            , byte precision = 0
            , byte scale = 0
            , bool output = false
            , ColumnFlags column_flags = ColumnFlags.None
            , bool? nullable = null
            , string? related_category_id = null
            , bool encrypted = false
            , bool isArray = false
            , string? override_with = null
            )
        {
            if (system_type == null && sql_type == null && value == null)
                throw new ArgumentNullException(nameof(sql_type), $"The parameter {nameof(sql_type)} can't be null if {nameof(system_type)} and {nameof(value)} are null.");

            // Determine sql_type
            if (sql_type == null)
            {
                sql_type = system_type?.ToSqlDbType() ?? value?.GetType().ToSqlDbType();
                if (sql_type == null)
                {
                    throw new ArgumentNullException(nameof(sql_type), $"Unable to determine the {nameof(sql_type)} from provided arguments.");
                }
            }

            // MMC: be aware that this wont work for strings a string may allways be null
            nullable ??= system_type != null ? Nullable.GetUnderlyingType(system_type) != null : value != null ? Nullable.GetUnderlyingType(value.GetType()) != null : null;

            nullable ??= false;

            // MMC: this is needed first before setting the values
            SQLMetadata = new SQLServerMetadata(sql_type.Value, size, precision, scale, output, (bool)nullable, encrypted, isArray);

            Name = name;
            ValueObject = value;
            SystemType = system_type!;

            ColumnMetadata = column_flags;

            RelatedCategoryID = related_category_id;
            OverrideWith = override_with;
        }


        public ColumnBase(ColumnBase col, string new_name = "", bool output = false)
            : this(
                  col.SystemType
                  , (string.IsNullOrEmpty(new_name) ? col.Name : new_name)
                  , col.ValueObject, col.SQLMetadata.SQLType
                  , col.SQLMetadata.Size
                  , col.SQLMetadata.Precision
                  , col.SQLMetadata.Scale
                  , output
                  , col.ColumnMetadata
                  , col.SQLMetadata.Nullable
                  , col.RelatedCategoryID
                  , col.SQLMetadata.Encrypted
                  , col.SQLMetadata.IsArray
                  , col.OverrideWith
                  )

        {
        }

        public override string ToString()
        {
            return Name;
        }

    }
}
