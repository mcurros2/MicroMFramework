using MicroM.Extensions;
using System.Data;
using System.Text.Json;

namespace MicroM.Data
{
    /// <summary>
    /// Represents base metadata and value handling for a database column.
    /// </summary>
    public abstract class ColumnBase
    {
        /// <summary>Gets the CLR type of the column value.</summary>
        public readonly Type SystemType = null!;

        /// <summary>Gets flags describing column behavior.</summary>
        public ColumnFlags ColumnMetadata { get; init; }

        /// <summary>Gets SQL Server metadata for the column.</summary>
        public SQLServerMetadata SQLMetadata { get; private set; }

        private string _Name = null!;

        /// <summary>Gets the column name.</summary>
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
        /// <summary>Gets or sets the raw value of the column.</summary>
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
        /// <summary>Gets the parameter name used in SQL commands.</summary>
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

        /// <summary>Gets the related category identifier if any.</summary>
        public string? RelatedCategoryID { get; init; }

        /// <summary>Gets the related status identifier if any.</summary>
        public string? RelatedStatusID { get; init; }

        /// <summary>Gets the server claim key used to override the value.</summary>
        public string? OverrideWith { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnBase"/> class.
        /// </summary>
        /// <param name="system_type">CLR type of the value.</param>
        /// <param name="name">Column name.</param>
        /// <param name="value">Column value.</param>
        /// <param name="sql_type">SQL type.</param>
        /// <param name="size">Column size.</param>
        /// <param name="precision">Numeric precision.</param>
        /// <param name="scale">Numeric scale.</param>
        /// <param name="output">Whether column is an output parameter.</param>
        /// <param name="column_flags">Column behavior flags.</param>
        /// <param name="nullable">Column allows nulls.</param>
        /// <param name="related_category_id">Related category identifier.</param>
        /// <param name="encrypted">Value is encrypted.</param>
        /// <param name="isArray">Value represents an array.</param>
        /// <param name="override_with">Server claim key to override value.</param>
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


        /// <summary>
        /// Initializes a new instance by copying an existing column.
        /// </summary>
        /// <param name="col">Source column.</param>
        /// <param name="new_name">Optional new name.</param>
        /// <param name="output">Whether column is an output parameter.</param>
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

        /// <summary>
        /// Returns the column name.
        /// </summary>
        /// <returns>Column name.</returns>
        public override string ToString()
        {
            return Name;
        }

    }
}
