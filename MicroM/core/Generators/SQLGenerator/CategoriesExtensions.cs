using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary;
using MicroM.DataDictionary.Configuration;
using MicroM.Extensions;
using MicroM.Generators.Extensions;
using System.Text;
using static MicroM.Generators.Constants;

namespace MicroM.Generators.SQLGenerator
{
    /// <summary>
    /// Extension methods that generate SQL snippets for handling entity
    /// categories.
    /// </summary>
    internal static class CategoriesExtensions
    {
        /// <summary>
        /// Returns two SQL scripts with the DDL to create a child category table and FK index to store related categories for an <see cref="Entity{TDefinition}"/> record.
        /// The category table name for the specified <seealso cref="Entity{TDefinition}"/> will be <![CDATA[<entity table name>_cat]]>.
        /// The columns will contain the <see cref="Entity{TDefinition}"/> primary keys + <seealso cref="Categories"/> primary keys + <see cref="DefaultColumns"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        internal static List<string> CreateCategoryTable<T>(this T entity) where T : EntityBase
        {
            return entity.CreateCategoryOrStatusTable(false);
        }

        /// <summary>
        /// Builds the SQL used to parse JSON category arrays into temporary
        /// tables for an entity.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="entity">Entity definition.</param>
        /// <param name="separator">Separator between generated statements.</param>
        /// <returns>SQL fragment or empty string if no categories are defined.</returns>
        internal static string AsJSONCategories<T>(this T entity, string separator = $"\n{TAB}{TAB}{TAB}, ") where T : EntityBase
        {
            if (entity.Def.RelatedCategories.Count == 0) return "";
            var sb = new StringBuilder();
            var Fakes = entity.Def.Columns.GetWithFlags(ColumnFlags.Fake, ColumnFlags.None);

            foreach (var cav_col in Fakes)
            {
                if (entity.Def.RelatedCategories.Contains(cav_col.RelatedCategoryID!) && cav_col.SQLMetadata.IsArray)
                {
                    var parms = new TemplateValues()
                    {
                        CATEGORY_TEMP_TABLE = $"[#Temp{cav_col.RelatedCategoryID}]",
                        CATEGORY_PARM = cav_col.AsProcParm(false),
                        CATEGORY = $"'{cav_col.RelatedCategoryID!.SQLEscape()}'"
                    };
                    sb.Append(Templates.JSON_CATEGORIES_PARSE_TEMPLATE.ReplaceTemplate(parms));
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Generates SQL to insert category records from JSON arrays for an entity.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="entity">Entity definition.</param>
        /// <param name="separator">Separator between values.</param>
        /// <returns>SQL fragment or empty string when not applicable.</returns>
        internal static string AsInsertJSONCategories<T>(this T entity, string separator = $"\n{TAB}{TAB}{TAB}, ") where T : EntityBase
        {
            if (entity.Def.RelatedCategories.Count == 0) return "";
            var sb = new StringBuilder();
            var Fakes = entity.Def.Columns.GetWithFlags(ColumnFlags.Fake, ColumnFlags.None);
            var PKs = entity.Def.Columns.GetWithFlags(ColumnFlags.PK);

            string entity_PK = PKs.AsProcParms(separator);
            foreach (var cav_col in Fakes)
            {
                if (entity.Def.RelatedCategories.Contains(cav_col.RelatedCategoryID!) && cav_col.SQLMetadata.IsArray)
                {
                    var parms = new TemplateValues()
                    {
                        CATEGORY_TEMP_TABLE = $"[#Temp{cav_col.RelatedCategoryID}]",
                        CATEGORIES_TABLE = $"[{entity.Def.TableName}{TableSuffix.CATEGORY_TABLE_SUFFIX}]",
                        INSERT_VALUES = $"{entity_PK}{separator}'{cav_col.RelatedCategoryID!.SQLEscape()}'{separator}jsoncategory_id",
                        CATEGORY_PARM = cav_col.AsProcParm(false),
                    };
                    sb.Append(Templates.INSERT_JSON_CAT_TEMPLATE.ReplaceTemplate(parms));
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Generates SQL to update category tables based on JSON arrays provided
        /// for an entity.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="entity">Entity definition.</param>
        /// <param name="union_string">Union string used in WHERE clauses.</param>
        /// <param name="separator">Separator between values.</param>
        /// <returns>SQL fragment or empty string when not applicable.</returns>
        internal static string AsUpdateJSONCategories<T>(this T entity, string union_string = $"\n{TAB}{TAB}{TAB}and ", string separator = $"\n{TAB}{TAB}{TAB}, ") where T : EntityBase
        {
            if (entity.Def.RelatedCategories.Count == 0) return "";
            var sb = new StringBuilder();
            var Fakes = entity.Def.Columns.GetWithFlags(ColumnFlags.Fake, ColumnFlags.None);
            var PKs = entity.Def.Columns.GetWithFlags(ColumnFlags.PK);

            string entity_PK = PKs.AsProcParms(separator);
            foreach (var cav_col in Fakes)
            {
                if (entity.Def.RelatedCategories.Contains(cav_col.RelatedCategoryID!) && cav_col.SQLMetadata.IsArray)
                {
                    var parms = new TemplateValues()
                    {
                        CATEGORY_TEMP_TABLE = $"[#Temp{cav_col.RelatedCategoryID}]",
                        CATEGORIES_TABLE = $"[{entity.Def.TableName}{TableSuffix.CATEGORY_TABLE_SUFFIX}]",
                        WHERE_CLAUSE = $"{PKs.AsColumnValuePairs(union_string: union_string)}{union_string}{nameof(CategoriesDef.c_category_id)} = '{cav_col.RelatedCategoryID!.SQLEscape()}'",
                        INSERT_VALUES = $"{entity_PK}{separator}'{cav_col.RelatedCategoryID!.SQLEscape()}'{separator}jsoncategory_id",
                    };
                    sb.Append(Templates.UPDATE_JSON_CAT_TEMPLATE.ReplaceTemplate(parms));
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Creates the VALUES clause for inserting category rows related to an
        /// entity.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="entity">Entity definition.</param>
        /// <param name="separator">Separator between values.</param>
        /// <returns>SQL fragment or empty string when not applicable.</returns>
        internal static string AsCategoriesInsertValues<T>(this T entity, string separator = $"\n{TAB}{TAB}{TAB}, ") where T : EntityBase
        {
            if (entity.Def.RelatedCategories.Count == 0) return "";
            var sb = new StringBuilder();
            var PKs = entity.Def.Columns.GetWithFlags(ColumnFlags.PK);
            var Fakes = entity.Def.Columns.GetWithFlags(ColumnFlags.Fake, ColumnFlags.None);

            string entity_PK = PKs.AsProcParms(separator);
            foreach (var cav_col in Fakes)
            {
                if (entity.Def.RelatedCategories.Contains(cav_col.RelatedCategoryID!) && cav_col.SQLMetadata.IsArray == false)
                {
                    var parms = new TemplateValues()
                    {
                        CATEGORIES_TABLE = $"[{entity.Def.TableName}{TableSuffix.CATEGORY_TABLE_SUFFIX}]",
                        INSERT_VALUES = $"{entity_PK}{separator}'{cav_col.RelatedCategoryID!.SQLEscape()}'{separator}{cav_col.AsProcParm(false)}",
                        CATEGORY_PARM = cav_col.AsProcParm(false)
                    };
                    if (cav_col.SQLMetadata.Nullable)
                    {
                        sb.Append(Templates.INSERT_CATEGORY_TEMPLATE_NULL.ReplaceTemplate(parms));
                    }
                    else
                    {
                        sb.Append(Templates.INSERT_CATEGORY_TEMPLATE.ReplaceTemplate(parms));
                    }
                }
            }
            return sb.ToString();
        }

        private static string AsCategoriesDeleteNull<T>(this T entity, ColumnBase related_category_column, string union_string = $"\n{TAB}{TAB}{TAB}{TAB}and ") where T : EntityBase
        {
            if (string.IsNullOrEmpty(related_category_column.RelatedCategoryID) || !related_category_column.SQLMetadata.Nullable) return "";
            var sb = new StringBuilder();
            var PKs = entity.Def.Columns.GetWithFlags(ColumnFlags.PK);

            var parms = new TemplateValues()
            {
                CATEGORIES_TABLE = $"[{entity.Def.TableName}{TableSuffix.CATEGORY_TABLE_SUFFIX}]",
                WHERE_CLAUSE = $"{PKs.AsColumnValuePairs(union_string: union_string)}{union_string}{nameof(Categories.Def.c_category_id)} = '{related_category_column.RelatedCategoryID.SQLEscape()}'",
                CATEGORY_PARM = related_category_column.AsProcParm(false)
            };
            sb.Append(Templates.DELETE_CATEGORY_NULL_TEMPLATE.ReplaceTemplate(parms));

            return sb.ToString();
        }


        /// <summary>
        /// Builds update statements for category tables using entity metadata.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="entity">Entity definition.</param>
        /// <param name="union_string">Union string used in WHERE clauses.</param>
        /// <param name="separator">Separator between values.</param>
        /// <returns>SQL fragment or empty string when not applicable.</returns>
        internal static string AsCategoriesUpdateTemplateValues<T>(this T entity, string union_string = $"\n{TAB}{TAB}{TAB}and ", string separator = $"\n{TAB}{TAB}{TAB}, ") where T : EntityBase
        {
            if (entity.Def.RelatedCategories.Count == 0) return "";

            var sb = new StringBuilder();

            var cat = new CategoriesValues();
            var PKs = entity.Def.Columns.GetWithFlags(ColumnFlags.PK);
            var Fakes = entity.Def.Columns.GetWithFlags(ColumnFlags.Fake, ColumnFlags.None);

            string entity_PK = PKs.AsProcParms(separator);
            foreach (var cav_col in Fakes)
            {
                if (entity.Def.RelatedCategories.Contains(cav_col.RelatedCategoryID!) && cav_col.SQLMetadata.IsArray == false)
                {
                    var parms = new TemplateValues()
                    {
                        CATEGORY_PARM = cav_col.AsProcParm(false),
                        CATEGORY_DELETE_NULL = entity.AsCategoriesDeleteNull(cav_col),
                        CATEGORIES_TABLE = $"[{entity.Def.TableName}{TableSuffix.CATEGORY_TABLE_SUFFIX}]",
                        INSERT_VALUES = $"{entity_PK}{separator}'{cav_col.RelatedCategoryID!.SQLEscape()}'{separator}{cav_col.AsProcParm(false)}",
                        UPDATE_VALUES = $"{cat.Def.c_categoryvalue_id.Name} = {cav_col.AsProcParm(false)}",
                        WHERE_CLAUSE = $"{PKs.AsColumnValuePairs(union_string: union_string)}{union_string}{cat.Def.c_category_id.Name} = '{cav_col.RelatedCategoryID!.SQLEscape()}'"
                    };
                    sb.Append(Templates.UPDATE_CATEGORY_TEMPLATE.ReplaceTemplate(parms));
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Generates the DELETE clause for removing related category rows when an
        /// entity record is dropped.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="entity">Entity definition.</param>
        /// <param name="separator">Separator used between conditions.</param>
        /// <returns>SQL fragment or empty string when not applicable.</returns>
        internal static string AsCategoriesDelete<T>(this T entity, string separator = $"\n{TAB}{TAB}{TAB}and ") where T : EntityBase
        {
            if (entity.Def.RelatedCategories.Count == 0) return "";

            var sb = new StringBuilder();
            var PKs = entity.Def.Columns.GetWithFlags(ColumnFlags.PK);

            var parms = new TemplateValues()
            {
                CATEGORIES_TABLE = $"[{entity.Def.TableName}{TableSuffix.CATEGORY_TABLE_SUFFIX}]",
                WHERE_CLAUSE = PKs.AsColumnValuePairs(union_string: $"\n{TAB}{TAB}{TAB}and ")
            };
            sb.Append(Templates.DELETE_CATEGORY_TEMPLATE.ReplaceTemplate(parms));

            return sb.ToString();
        }

        /// <summary>
        /// Builds SQL to return category values as JSON arrays for an entity.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="entity">Entity definition.</param>
        /// <param name="union_string">Union string used in WHERE clauses.</param>
        /// <returns>SQL fragment or empty string when not applicable.</returns>
        internal static string AsJSONCategoriesGet<T>(this T entity, string union_string = $"\n{TAB}{TAB}and ") where T : EntityBase
        {
            if (entity.Def.RelatedCategories.Count == 0) return "";
            var sb = new StringBuilder();

            var PKs = entity.Def.Columns.GetWithFlags(ColumnFlags.PK);
            var Fakes = entity.Def.Columns.GetWithFlags(ColumnFlags.Fake, ColumnFlags.None);

            foreach (var cav_col in Fakes)
            {
                if (entity.Def.RelatedCategories.Contains(cav_col.RelatedCategoryID!) && cav_col.SQLMetadata.IsArray)
                {

                    var parms = new TemplateValues()
                    {
                        CATEGORY_PARM = cav_col.AsProcParm(false),
                        CATEGORIES_TABLE = $"[{entity.Def.TableName}{TableSuffix.CATEGORY_TABLE_SUFFIX}]",
                        WHERE_CLAUSE = $"{PKs.AsColumnValuePairs(union_string: union_string)}{union_string}{nameof(CategoriesDef.c_category_id)} = '{cav_col.RelatedCategoryID!.SQLEscape()}'",
                    };
                    sb.Append(Templates.JSON_CATEGORY_GET_TEMPLATE.ReplaceTemplate(parms));
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Produces the variable declarations required to hold JSON category
        /// arrays when retrieving a record.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="entity">Entity definition.</param>
        /// <returns>SQL declarations or empty string when not applicable.</returns>
        internal static string AsJSONCategoriesGetParmsDeclaration<T>(this T entity) where T : EntityBase
        {
            if (entity.Def.RelatedCategories.Count == 0) return "";
            var sb = new StringBuilder();

            //var PKs = entity.Def.Columns.GetWithFlags(ColumnFlags.PK);
            var Fakes = entity.Def.Columns.GetWithFlags(ColumnFlags.Fake, ColumnFlags.None);

            bool declared = false;
            foreach (var cav_col in Fakes)
            {
                if (entity.Def.RelatedCategories.Contains(cav_col.RelatedCategoryID!) && cav_col.SQLMetadata.IsArray)
                {
                    if (!declared)
                    {
                        sb.Append("declare ");
                        declared = true;
                    }
                    else
                    {
                        sb.Append(", ");
                    }
                    sb.Append(cav_col.AsProcParm(true));
                }
            }
            return sb.ToString();
        }




    }
}
