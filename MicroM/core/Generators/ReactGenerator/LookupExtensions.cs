using MicroM.Core;
using MicroM.Data;
using MicroM.Generators.Extensions;
using System.Globalization;
using System.Text;
using static MicroM.Generators.Constants;

namespace MicroM.Generators.ReactGenerator
{
    /// <summary>
    /// Extension methods for generating lookup definitions used by the React client.
    /// </summary>
    public static class LookupExtensions
    {
        private static void AppendDefaultLookupDefinition(this StringBuilder sb, EntityForeignKeyBase fk, string indent)
        {
            sb.Append(CultureInfo.InvariantCulture, $"\n{indent}{TAB}'{fk.ParentEntityType.Name}': {{\n{indent}{TAB}{TAB}name: '{fk.ParentEntityType.Name}',");
            sb.Append(CultureInfo.InvariantCulture, $"\n{indent}{TAB}{TAB}viewMapping: {{ keyIndex: 0, descriptionIndex: 1 }},");
            sb.Append(CultureInfo.InvariantCulture, $"\n{indent}{TAB}{TAB}entityConstructor: (client: MicroMClient, parentKeys?: ValuesObject) => new {fk.ParentEntityType.Name}(client, parentKeys)");
            sb.Append(CultureInfo.InvariantCulture, $"\n{indent}{TAB}}}");
        }

        /// <summary>
        /// Generates default lookup definitions for the provided foreign keys.
        /// </summary>
        /// <param name="foreign_keys">Foreign keys whose parent entities define default lookups.</param>
        /// <param name="indent">Indentation applied to each generated line.</param>
        /// <returns>A string containing formatted lookup definitions for the default lookups.</returns>
        public static string AsDefaultLookupDefinitionContent(this IReadOnlyDictionary<string, EntityForeignKeyBase> foreign_keys, string indent = $"{TAB}")
        {
            var lookups_enumerator = foreign_keys.Values.GetEnumerator();

            StringBuilder sb = new();

            if (lookups_enumerator.MoveNext())
            {
                sb.AppendDefaultLookupDefinition(lookups_enumerator.Current, indent);

                while (lookups_enumerator.MoveNext())
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, ",\n{0}", indent);
                    sb.AppendDefaultLookupDefinition(lookups_enumerator.Current, indent);
                }
            }

            return sb.ToString();
        }

        private static void AppendLookupDefinition(this StringBuilder sb, KeyValuePair<string, EntityLookup> kvp, EntityForeignKeyBase fk, string indent)
        {
            EntityLookup lookup = kvp.Value;
            string lookup_name = kvp.Key;
            sb.Append(CultureInfo.InvariantCulture, $"\n{indent}{TAB}'{lookup_name}': {{\n{indent}{TAB}{TAB}name: '{lookup_name}',");
            sb.Append(CultureInfo.InvariantCulture, $"\n{indent}{TAB}{TAB}viewMapping: {{ keyIndex: {lookup.IDColumnIndex}, descriptionIndex: {lookup.DescriptionColumnIndex} }},");
            sb.Append(CultureInfo.InvariantCulture, $"\n{indent}{TAB}{TAB}entityConstructor: (client: MicroMClient, parentKeys?: ValuesObject) => new {fk.ParentEntityType.Name}(client, parentKeys)");
            sb.Append(CultureInfo.InvariantCulture, $"\n{indent}{TAB}}}");
        }

        /// <summary>
        /// Generates lookup definitions for explicit lookups defined on the provided foreign keys.
        /// </summary>
        /// <param name="foreign_keys">Foreign keys that contain lookup definitions.</param>
        /// <param name="indent">Indentation applied to each generated line.</param>
        /// <returns>A string containing formatted lookup definitions for the configured lookups.</returns>
        public static string AsLookupDefinitionContent(this IReadOnlyDictionary<string, EntityForeignKeyBase> foreign_keys, string indent = $"{TAB}")
        {
            var lookups_enumerator = foreign_keys.Values.SelectMany(
                fk => fk.EntityLookups.AsEnumerable(),
                (fk, kvp) => new { ForeignKey = fk, EntityLookupPair = kvp }
            ).GetEnumerator();

            StringBuilder sb = new();

            if (lookups_enumerator.MoveNext())
            {
                sb.AppendLookupDefinition(lookups_enumerator.Current.EntityLookupPair, lookups_enumerator.Current.ForeignKey, indent);

                while (lookups_enumerator.MoveNext())
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, ",\n{0}", indent);
                    sb.AppendLookupDefinition(lookups_enumerator.Current.EntityLookupPair, lookups_enumerator.Current.ForeignKey, indent);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds the lookup definitions for an entity, combining foreign key and category lookups.
        /// </summary>
        /// <param name="def">The entity definition to generate lookups for.</param>
        /// <param name="indent">Indentation applied to each generated line.</param>
        /// <returns>A string containing the lookup definition snippet, or an empty string if no lookups are present.</returns>
        public static string AsLookupDefinition(this EntityDefinition def, string indent = $"{TAB}")
        {
            string? fk_default_lookups = def.ForeignKeys.Count > 0 ? def.ForeignKeys.AsDefaultLookupDefinitionContent() : null;
            string? fk_lookups = def.ForeignKeys.Count > 0 ? def.ForeignKeys.AsLookupDefinitionContent() : null;
            string? cat_lookups = def.Columns.AsLookupDefinitionContentCategories();

            bool has_lookups = !string.IsNullOrEmpty(fk_lookups) || !string.IsNullOrEmpty(cat_lookups) || !string.IsNullOrEmpty(fk_default_lookups);

            if (has_lookups)
            {
                var parms = new TemplateValues()
                {
                    LOOKUPS_DEFINITION = $"{(has_lookups ? "{" : "")}{fk_default_lookups ?? ""}{(fk_default_lookups != null && fk_default_lookups.Length > 0 ? $",\n{indent}" : "")}{fk_lookups ?? ""}{fk_lookups ?? ""}{(fk_lookups != null && fk_lookups.Length > 0 ? $",\n{indent}" : "")}{cat_lookups ?? ""}{(has_lookups ? $"\n{indent}}}" : "")}"
                };
                return Templates.ENTITY_LOOKUPS_TEMPLATE.ReplaceTemplate(parms);
            }
            return "";
        }

    }
}
