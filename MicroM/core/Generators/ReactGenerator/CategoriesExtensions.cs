using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.Configuration;
using MicroM.Generators.Extensions;
using System.Globalization;
using System.Text;
using static MicroM.Generators.Constants;

namespace MicroM.Generators.ReactGenerator
{
    /// <summary>
    /// Provides helpers for generating TypeScript code related to entity
    /// categories when building the React client.
    /// </summary>
    public static class CategoriesExtensions
    {
        private static void AppendLookupDefinitionContentCategories(this StringBuilder sb, ColumnBase col, string indent = $"{TAB}")
        {
            var lookup_name = col.RelatedCategoryID;

            sb.Append(CultureInfo.InvariantCulture, $"\n{indent}{TAB}'{lookup_name}': {{\n{indent}{TAB}{TAB}name: '{lookup_name}',");
            sb.Append(CultureInfo.InvariantCulture, $"\n{indent}{TAB}{TAB}viewMapping: {{ keyIndex: 0, descriptionIndex: 1 }},");
            sb.Append(CultureInfo.InvariantCulture, $"\n{indent}{TAB}{TAB}entityConstructor: (client: MicroMClient, parentKeys?: ValuesObject) => new cat{lookup_name}(client, parentKeys)");
            sb.Append(CultureInfo.InvariantCulture, $"\n{indent}{TAB}}}");
        }

        /// <summary>
        /// Builds the lookup definition block for category columns.
        /// </summary>
        /// <param name="cols">Collection of columns to inspect for category relationships.</param>
        /// <param name="indent">Indentation used in the generated template.</param>
        /// <returns>TypeScript code containing lookup definitions for categories.</returns>
        public static string AsLookupDefinitionContentCategories(this IReadonlyOrderedDictionary<ColumnBase> cols, string indent = $"{TAB}")
        {
            var cols_enumerator = cols.Values.Where(column => string.IsNullOrEmpty(column.RelatedCategoryID) == false).GetEnumerator();

            StringBuilder sb = new();

            if (cols_enumerator.MoveNext())
            {
                sb.AppendLookupDefinitionContentCategories(cols_enumerator.Current, indent);

                while (cols_enumerator.MoveNext())
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, ",\n{0}", indent);
                    sb.AppendLookupDefinitionContentCategories(cols_enumerator.Current, indent);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates the list of category entities that need to be imported.
        /// </summary>
        /// <param name="cols">Collection of columns to inspect for category relationships.</param>
        /// <returns>A comma separated list of category entity imports.</returns>
        public static string AsEmbeddedCategoriesImport(this IReadonlyOrderedDictionary<ColumnBase> cols)
        {
            var relatedCategories = string.Join(", ",
                cols.Values
                    .Where(column => !string.IsNullOrEmpty(column.RelatedCategoryID))
                    .Select(column => $"cat{column.RelatedCategoryID}"));
            return relatedCategories;
        }

        private static string GetCategoryTitle(this Dictionary<string, Type> categories_types, string category_id)
        {
            string cat_title = "";
            if (categories_types.TryGetValue(category_id, out Type? category_type))
            {
                CategoryDefinition? cat = (CategoryDefinition?)Activator.CreateInstance(category_type);
                if (cat != null) cat_title = cat.Description;
            }
            return cat_title;
        }

        private static string AppendCategoryEntity(this Dictionary<string, Type> categories_types, string category_id)
        {
            string title = categories_types.GetCategoryTitle(category_id);
            var parms = new TemplateValues()
            {
                CATEGORY_ID = category_id,
                CATEGORY_TITLE = title,
                MICROM_LIB_PACKAGE = TemplateValues.CONST_MICROM_LIB_PACKAGE
            };
            return Templates.CATEGORY_ENTITY_TEMPLATE.ReplaceTemplate(parms);
        }

        /// <summary>
        /// Creates TypeScript entity classes for the categories referenced by the columns.
        /// </summary>
        /// <param name="cols">Collection of columns that may reference categories.</param>
        /// <param name="categories_types">Dictionary mapping category identifiers to their definition types.</param>
        /// <returns>TypeScript code defining the category entities.</returns>
        public static string AsCategoriesEntities(this IReadonlyOrderedDictionary<ColumnBase> cols, Dictionary<string, Type> categories_types)
        {
            var cols_enumerator = cols.Values.Where(column => string.IsNullOrEmpty(column.RelatedCategoryID) == false).GetEnumerator();

            StringBuilder sb = new();

            if (cols_enumerator.MoveNext())
            {
                sb.Append(categories_types.AppendCategoryEntity(cols_enumerator.Current.RelatedCategoryID!));

                while (cols_enumerator.MoveNext())
                {
                    sb.Append("\n\n/*-------------------------------------------------------------------------*/");
                    sb.Append(categories_types.AppendCategoryEntity(cols_enumerator.Current.RelatedCategoryID!));
                }
            }

            return sb.ToString().RemoveEmptyLines();
        }


    }
}
