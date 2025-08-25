using System.Data;

namespace MicroM.Data
{
    /// <summary>
    /// Represents a typed database column and related factory helpers.
    /// </summary>
    /// <typeparam name="T">Type of the column value.</typeparam>
    public class Column<T> : ColumnBase
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="Column{T}"/> class.
        /// </summary>
        /// <param name="name">destination_name is usually not specified and obtained through reflection</param>
        /// <param name="value"></param>
        /// <param name="sql_type">The SQL Type </param>
        /// <param name="size"></param>
        /// <param name="precision">The total number of digits</param>
        /// <param name="scale">The decimal digits</param>
        /// <param name="output"></param>
        /// <param name="column_flags"></param>
        /// <param name="nullable"></param>
        /// <param name="fake"></param>
        /// <param name="encrypted"></param>
        /// <param name="isArray"></param>
        /// <param name="override_with"></param>
        public Column(
            string name = ""
            , T value = default!
            , SqlDbType? sql_type = null
            , int size = 0
            , byte precision = 0
            , byte scale = 0
            , bool output = false
            , ColumnFlags column_flags = ColumnFlags.Insert | ColumnFlags.Update
            , bool? nullable = null
            , bool fake = false
            , bool encrypted = false
            , bool isArray = false
            , string? override_with = null
            )
            : base(typeof(T), name, value, sql_type, size, precision, scale, output, column_flags, nullable, related_category_id: default, encrypted, isArray, override_with)
        {
            if (fake) ColumnMetadata |= ColumnFlags.Fake;
        }

        /// <summary>
        /// Initializes a new instance by copying an existing column.
        /// </summary>
        /// <param name="original">Source column.</param>
        /// <param name="new_name">Optional new name.</param>
        /// <param name="output">Whether column is an output parameter.</param>
        public Column(Column<T> original, string new_name = "", bool output = false)
            : base(original, new_name, output)
        {
        }

        // Columns creation factories

        /// <summary>
        /// Creates a primary key column definition.
        /// </summary>
        public static Column<T> PK(string name = "", SqlDbType? sql_type = null, int size = 20, byte precision = 0, byte scale = 0,
            T value = default!, bool autonum = false, bool fake = false, string? override_with = null)
        {
            ColumnFlags col_flags = ColumnFlags.Insert | ColumnFlags.Update | ColumnFlags.Delete | ColumnFlags.Get | ColumnFlags.PK;
            if (autonum) col_flags |= ColumnFlags.Autonum;
            if (fake) col_flags |= ColumnFlags.Fake;

            if (sql_type == null)
            {
                if (typeof(T) == typeof(string))
                {
                    sql_type = SqlDbType.Char;
                }
                else
                {
                    sql_type = typeof(T).ToSqlDbType();
                }
            }

            Column <T> col = new(name, value: value, sql_type: sql_type, size: size, precision: precision,
                scale: scale, column_flags: col_flags, override_with: override_with);
            return col;
        }

        /// <summary>
        /// Creates a foreign key column definition.
        /// </summary>
        public static Column<T> FK(string name = "", SqlDbType? sql_type = null, int size = 20, byte precision = 0, byte scale = 0, T value = default!, bool fake = false
            , bool? nullable = null, string? override_with = null)
        {
            ColumnFlags flags = ColumnFlags.Insert | ColumnFlags.Update | ColumnFlags.FK;
            if (fake) flags |= ColumnFlags.Fake;

            if (sql_type == null)
            {
                if (typeof(T) == typeof(string))
                {
                    sql_type = SqlDbType.Char;
                }
                else
                {
                    sql_type = typeof(T).ToSqlDbType();
                }
            }

            return new Column<T>(name, value: value, sql_type: sql_type, size: size, precision: precision,
                scale: scale, column_flags: flags, nullable: nullable, override_with: override_with);
        }

        /// <summary>
        /// Creates a text column definition.
        /// </summary>
        public static Column<T> Text(T value = default!, int size = 255, bool fake = false, bool? nullable = null, bool isArray = false, bool encrypted = false
            , ColumnFlags column_flags = ColumnFlags.Insert | ColumnFlags.Update, string? override_with = null)
        {
            if (typeof(T) != typeof(string) && typeof(T) != typeof(string[]) && Nullable.GetUnderlyingType(typeof(T)) != typeof(string[]))
            {
                throw new ArgumentException($"Invalid type {typeof(T)}. Only string, string[], or nullable string[] are allowed.");
            }

            if (fake) column_flags |= ColumnFlags.Fake;
            return new Column<T>("", value: value, sql_type: SqlDbType.VarChar, size: size, column_flags: column_flags, nullable: nullable, isArray: isArray, encrypted: encrypted, override_with: override_with);
        }

        /// <summary>
        /// Creates a fixed-length character column definition.
        /// </summary>
        public static Column<T> Char(T value = default!, int size = 255, bool fake = false, bool? nullable = null, bool isArray = false, string? override_with = null)
        {
            if (typeof(T) != typeof(string) && typeof(T) != typeof(string[]) && Nullable.GetUnderlyingType(typeof(T)) != typeof(string[]))
            {
                throw new ArgumentException($"Invalid type {typeof(T)}. Only string, string[], or nullable string[] are allowed.");
            }

            ColumnFlags flags = ColumnFlags.Insert | ColumnFlags.Update;
            if (fake) flags |= ColumnFlags.Fake;
            return new Column<T>("", value: value, sql_type: SqlDbType.Char, size: size, column_flags: flags, nullable: nullable, isArray: isArray, override_with: override_with);
        }

        // MMC: Decide about datetime offset, datetime2, etc.

        //public static Column<T> Date(T value = default!, int size = 255, bool fake = false, bool? nullable = null, ColumnFlags column_flags = ColumnFlags.Insert | ColumnFlags.Update)
        //{
        //    if (typeof(T) != typeof(DateTime) && typeof(T) != typeof(DateOnly) && typeof(T) != typeof(DateTimeOffset))
        //    {
        //        throw new ArgumentException($"Invalid type {typeof(T)}. Only DateTime, DateOnly, DateTimeOffset are allowed.");
        //    }

        //    if (fake) column_flags |= ColumnFlags.Fake;
        //    return new Column<T>("", value: value, sql_type: SqlDbType.VarChar, size: size, column_flags: column_flags, nullable: nullable);
        //}


        //
        /// <summary>
        /// Creates a copy of this column.
        /// </summary>
        public Column<T> Clone()
        {
            return new Column<T>(this);
        }



        /// <summary>
        /// Creates a column embedding a category identifier.
        /// </summary>
        public static Column<T> EmbedCategory(object category_id, bool nullable = false, bool isArray = false, T value = default!)
        {
            if (typeof(T) != typeof(string) && typeof(T) != typeof(string[]) && Nullable.GetUnderlyingType(typeof(T)) != typeof(string[]))
            {
                throw new ArgumentException($"Invalid type {typeof(T)}. Only string, string[], or nullable string[] are allowed.");
            }

            isArray = (typeof(T) == typeof(string[]) || Nullable.GetUnderlyingType(typeof(T)) == typeof(string[]));

            ColumnFlags column_flags = ColumnFlags.Insert | ColumnFlags.Update | ColumnFlags.Fake;
            if (isArray)
            {
                return new Column<T>("", value: (T)(object)value!, sql_type: SqlDbType.VarChar, size: 0, column_flags: column_flags, nullable: nullable, isArray: isArray) { RelatedCategoryID = (string)category_id };
            }
            else
            {
                return new Column<T>("", value: value, sql_type: SqlDbType.Char, size: 20, column_flags: column_flags, nullable: nullable, isArray: isArray) { RelatedCategoryID = (string)category_id };
            }
        }

        /// <summary>
        /// Creates a column embedding a status identifier.
        /// </summary>
        public static Column<string> EmbedStatus(string status_id, string value = default!)
        {
            if (typeof(T) != typeof(string))
            {
                throw new ArgumentException($"Invalid type {typeof(T)}. Only string, is allowed.");
            }

            ColumnFlags column_flags = ColumnFlags.Insert | ColumnFlags.Update | ColumnFlags.Fake;
            return new Column<string>("", value: value, sql_type: SqlDbType.Char, size: 20, column_flags: column_flags) { RelatedStatusID = status_id };
        }

        /// <summary>Gets or sets the typed value of the column.</summary>
        public T Value
        {
            get
            {
                if (ValueObject == null)
                {
                    Type? underlyingType = Nullable.GetUnderlyingType(typeof(T));

                    // Check if T is nullable or a reference type
                    if (underlyingType != null || !typeof(T).IsValueType)
                    {
                        return (T)(object)null!;
                    }
                    else if (typeof(T) == typeof(string))
                    {
                        // If T is a string, return an empty string as default
                        return (T)(object)string.Empty;
                    }
                    else
                    {
                        // T is a non-nullable value type, so return default(T)
                        return default!;
                    }
                }

                return (T)ValueObject;
            }
            set => this.ValueObject = value;
        }


    }

}
