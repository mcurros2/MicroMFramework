using MicroM.Core;
using MicroM.Generators.Extensions;

namespace MicroM.Generators.ReactGenerator
{
    /// <summary>
    /// Extension methods for generating TypeScript code for React client
    /// entities.
    /// </summary>
    public static class EntityExtensions
    {
        /// <summary>
        /// Creates the TypeScript entity definition class for the specified entity.
        /// </summary>
        /// <typeparam name="T">Type of the entity.</typeparam>
        /// <param name="entity">Entity used to build the definition.</param>
        /// <returns>TypeScript code for the entity definition.</returns>
        public static string AsTypeScriptEntityDefinition<T>(this T entity) where T : EntityBase
        {
            string lookup_definitions = entity.Def.AsLookupDefinition();
            string categories_import = entity.Def.Columns.AsEmbeddedCategoriesImport();
            string procs_definitions = entity.Def.AsProcsDefinition();

            var parms = new TemplateValues()
            {

                MNEO = entity.Def.Mneo,
                ENTITY_CLASSNAME = entity.Def.Name,
                COLUMNS_DEFINITION = entity.Def.Columns.AsTypeScriptColumnsDefinition(),
                VIEWS_DEFINITION = entity.Def.Views.AsViewsDefinition(),
                ENTITY_LOOKUPS_DEFINITION = lookup_definitions,
                ENTITY_LOOKUPS_ASSIGNMENT = !string.IsNullOrEmpty(lookup_definitions) ? TemplateValues.CONST_LOOKUPS_ASSIGNMENT : "",
                ENTITY_LOOKUP_IMPORT = !string.IsNullOrEmpty(lookup_definitions) ? TemplateValues.CONST_ENTITY_LOOKUP_IMPORT : "",
                ENTITY_PROC_DEFINITIONS = procs_definitions,
                ENTITY_PROCS_ASSIGNMENT = !string.IsNullOrEmpty(procs_definitions) ? TemplateValues.CONST_PROCS_ASSIGNMENT : "",
                EMBEDDED_CATEGORIES_IMPORT = !string.IsNullOrEmpty(categories_import) ? $"{categories_import}" : "",
                MICROM_LIB_PACKAGE = TemplateValues.CONST_MICROM_LIB_PACKAGE
            };
            return Templates.ENTITY_DEFINITION_TEMPLATE.ReplaceTemplate(parms).RemoveEmptyLines();
        }

        /// <summary>
        /// Creates the TypeScript entity class for the specified entity definition.
        /// </summary>
        /// <typeparam name="T">Type of the entity.</typeparam>
        /// <param name="entity">Entity used to generate the class.</param>
        /// <returns>TypeScript code for the entity class.</returns>
        public static string AsTypeScriptEntity<T>(this T entity) where T : EntityBase
        {
            var parms = new TemplateValues()
            {
                ENTITY_CLASSNAME = entity.Def.Name,
                ENTITY_TITLE = entity.Def.Name.AddSpacesAndLowercaseShortWords(),
                MICROM_LIB_PACKAGE = TemplateValues.CONST_MICROM_LIB_PACKAGE
            };
            return Templates.ENTITY_TEMPLATE.ReplaceTemplate(parms).RemoveEmptyLines();
        }


        //-----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Generates the React form component for the entity.
        /// </summary>
        /// <typeparam name="T">Type of the entity.</typeparam>
        /// <param name="entity">Entity for which to build the form.</param>
        /// <returns>TypeScript code for the entity form component.</returns>
        public static string AsTypeScriptEntityForm<T>(this T entity) where T : EntityBase
        {
            var parms = new TemplateValues()
            {
                ENTITY_CLASSNAME = entity.Def.Name,
                FIELDS_CONTROLS = entity.Def.Columns.AsTypeScriptFieldsControls(),
                FIELDS_IMPORT = entity.Def.Columns.AsFieldsImport(),
            };
            return Templates.ENTITY_FORM_TEMPLATE.ReplaceTemplate(parms).RemoveEmptyLines();
        }


    }
}
