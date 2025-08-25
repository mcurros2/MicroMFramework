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
    /// Extension methods used to generate lookup definitions and category
    /// entity scaffolding for the React code generator.
    /// </summary>
    public static class CategoriesExtensions
    {
        /// <summary>
        /// Appends the lookup definition for the related category of the given
        /// column to the provided <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="sb">Builder that collects lookup definition content.</param>
        /// <param name="col">Column containing the related category identifier.</param>
        /// <param name="indent">Indentation used when formatting the output.</param>
        private static void AppendLookupDefinitionContentCategories(this StringBuilder sb, ColumnBase col, string indent = $"{TAB}")
        {
            var lookup_name = col.RelatedCategoryID;

            sb.Append(CultureInfo.InvariantCulture, $"\n{indent}{TAB}'{lookup_name}': {{\n{indent}{TAB}{TAB}name: '{lookup_name}',");
            sb.Append(CultureInfo.InvariantCulture, $"\n{indent}{TAB}{TAB}viewMapping: {{ keyIndex: 0, descriptionIndex: 1 }},");
            sb.Append(CultureInfo.InvariantCulture, $"\n{indent}{TAB}{TAB}entityConstructor: (client: MicroMClient, parentKeys?: ValuesObject) => new cat{lookup_name}(client, parentKeys)");
            sb.Append(CultureInfo.InvariantCulture, $"\n{indent}{TAB}}}");
        }

        /// <summary>
        /// Generates lookup definition content for all columns that reference a
        /// category.
        /// </summary>
        /// <param name="cols">Collection of columns to inspect.</param>
        /// <param name="indent">Indentation used when formatting the output.</param>
        /// <returns>Lookup definition content for all related categories.</returns>
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
        /// Builds a comma-separated list of category entity imports for use in
        /// generated React code.
        /// </summary>
        /// <param name="cols">Collection of columns to inspect.</param>
        /// <returns>Comma-separated list of category entity class names.</returns>
        public static string AsEmbeddedCategoriesImport(this IReadonlyOrderedDictionary<ColumnBase> cols)
        {
            var relatedCategories = string.Join(", ",
                cols.Values
                    .Where(column => !string.IsNullOrEmpty(column.RelatedCategoryID))
                    .Select(column => $"cat{column.RelatedCategoryID}"));
            return relatedCategories;
        }

        /// <summary>
        /// Resolves the human-readable title for the specified category
        /// identifier by instantiating its definition type.
        /// </summary>
        /// <param name="categories_types">Dictionary mapping category identifiers to their definition types.</param>
        /// <param name="category_id">Identifier of the category to resolve.</param>
        /// <returns>The category description or an empty string if not found.</returns>
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

        /// <summary>
        /// Generates a single category entity using the template system.
        /// </summary>
        /// <param name="categories_types">Dictionary mapping category identifiers to their definition types.</param>
        /// <param name="category_id">Identifier of the category to generate.</param>
        /// <returns>Generated category entity code.</returns>
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
        /// Generates entity class definitions for all categories referenced by
        /// the provided columns.
        /// </summary>
        /// <param name="cols">Collection of columns to inspect.</param>
        /// <param name="categories_types">Dictionary mapping category identifiers to their definition types.</param>
        /// <returns>Formatted string with generated category entities.</returns>
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
