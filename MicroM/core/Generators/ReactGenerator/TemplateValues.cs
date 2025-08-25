namespace MicroM.Generators.ReactGenerator
{
    /// <summary>
    /// Holds tokens used to generate React TypeScript entities and related templates.
    /// Each property represents a placeholder value replaced during template rendering.
    /// </summary>
    internal class TemplateValues : TemplateValuesBase
    {
        /// <summary>
        /// Default package name for the MicroM client library.
        /// </summary>
        public const string CONST_MICROM_LIB_PACKAGE = "@mcurros2/microm";
        /// <summary>
        /// Import tokens appended when lookup support is required.
        /// </summary>
        public const string CONST_ENTITY_LOOKUP_IMPORT = ", MicroMClient, ValuesObject";
        /// <summary>
        /// Statement used to assign lookup definitions to the entity.
        /// </summary>
        public const string CONST_LOOKUPS_ASSIGNMENT = "lookups = lookups();";
        /// <summary>
        /// Statement used to assign procedure definitions to the entity.
        /// </summary>
        public const string CONST_PROCS_ASSIGNMENT = "procs = procs();";

        /// <summary>
        /// Mnemonic code identifying the entity.
        /// </summary>
        public string MNEO { get => tokens[nameof(MNEO)]; init => tokens[nameof(MNEO)] = value; }
        /// <summary>
        /// TypeScript definition for the entity's columns.
        /// </summary>
        public string COLUMNS_DEFINITION { get => tokens[nameof(COLUMNS_DEFINITION)]; init => tokens[nameof(COLUMNS_DEFINITION)] = value; }
        /// <summary>
        /// Mapping of view keys to entity properties.
        /// </summary>
        public string VIEW_KEY_MAPPINGS { get => tokens[nameof(VIEW_KEY_MAPPINGS)]; init => tokens[nameof(VIEW_KEY_MAPPINGS)] = value; }
        /// <summary>
        /// Definitions of lookup tables used by the entity.
        /// </summary>
        public string ENTITY_LOOKUPS_DEFINITION { get => tokens[nameof(ENTITY_LOOKUPS_DEFINITION)]; init => tokens[nameof(ENTITY_LOOKUPS_DEFINITION)] = value; }
        /// <summary>
        /// Name of the generated entity class.
        /// </summary>
        public string ENTITY_CLASSNAME { get => tokens[nameof(ENTITY_CLASSNAME)]; init => tokens[nameof(ENTITY_CLASSNAME)] = value; }
        /// <summary>
        /// Assignment statement for the lookup definitions.
        /// </summary>
        public string ENTITY_LOOKUPS_ASSIGNMENT { get => tokens[nameof(ENTITY_LOOKUPS_ASSIGNMENT)]; init => tokens[nameof(ENTITY_LOOKUPS_ASSIGNMENT)] = value; }
        /// <summary>
        /// Token representing the entity form component name.
        /// </summary>
        public string ENTITY_FORM { get => tokens[nameof(ENTITY_FORM)]; init => tokens[nameof(ENTITY_FORM)] = value; }
        /// <summary>
        /// Additional imports required for lookup support.
        /// </summary>
        public string ENTITY_LOOKUP_IMPORT { get => tokens[nameof(ENTITY_LOOKUP_IMPORT)]; init => tokens[nameof(ENTITY_LOOKUP_IMPORT)] = value; }
        /// <summary>
        /// TypeScript definition for the entity's views.
        /// </summary>
        public string VIEWS_DEFINITION { get => tokens[nameof(VIEWS_DEFINITION)]; init => tokens[nameof(VIEWS_DEFINITION)] = value; }
        /// <summary>
        /// Name of a stored procedure referenced in templates.
        /// </summary>
        public string PROC_NAME { get => tokens[nameof(PROC_NAME)]; init => tokens[nameof(PROC_NAME)] = value; }
        /// <summary>
        /// Definition block for lookup values.
        /// </summary>
        public string LOOKUPS_DEFINITION { get => tokens[nameof(LOOKUPS_DEFINITION)]; init => tokens[nameof(LOOKUPS_DEFINITION)] = value; }
        /// <summary>
        /// Package name used for MicroM imports.
        /// </summary>
        public string MICROM_LIB_PACKAGE { get => tokens[nameof(MICROM_LIB_PACKAGE)]; init => tokens[nameof(MICROM_LIB_PACKAGE)] = value; }
        /// <summary>
        /// Import statement for embedded categories used by the entity.
        /// </summary>
        public string EMBEDDED_CATEGORIES_IMPORT { get => tokens[nameof(EMBEDDED_CATEGORIES_IMPORT)]; init => tokens[nameof(EMBEDDED_CATEGORIES_IMPORT)] = value; }
        /// <summary>
        /// Identifier for a generated category entity.
        /// </summary>
        public string CATEGORY_ID { get => tokens[nameof(CATEGORY_ID)]; init => tokens[nameof(CATEGORY_ID)] = value; }
        /// <summary>
        /// Comma-separated list of related category entity names.
        /// </summary>
        public string RELATED_CATEGORIES_ENTITIES { get => tokens[nameof(RELATED_CATEGORIES_ENTITIES)]; init => tokens[nameof(RELATED_CATEGORIES_ENTITIES)] = value; }
        /// <summary>
        /// Display title for a category.
        /// </summary>
        public string CATEGORY_TITLE { get => tokens[nameof(CATEGORY_TITLE)]; init => tokens[nameof(CATEGORY_TITLE)] = value; }
        /// <summary>
        /// Imports required for form field components.
        /// </summary>
        public string FIELDS_IMPORT { get => tokens[nameof(FIELDS_IMPORT)]; init => tokens[nameof(FIELDS_IMPORT)] = value; }
        /// <summary>
        /// JSX controls rendered within the entity form.
        /// </summary>
        public string FIELDS_CONTROLS { get => tokens[nameof(FIELDS_CONTROLS)]; init => tokens[nameof(FIELDS_CONTROLS)] = value; }
        /// <summary>
        /// Display title for the entity.
        /// </summary>
        public string ENTITY_TITLE { get => tokens[nameof(ENTITY_TITLE)]; init => tokens[nameof(ENTITY_TITLE)] = value; }
        /// <summary>
        /// Code block defining stored procedures in an entity definition.
        /// </summary>
        public string ENTITY_PROC_DEFINITIONS { get => tokens[nameof(ENTITY_PROC_DEFINITIONS)]; init => tokens[nameof(ENTITY_PROC_DEFINITIONS)] = value; }
        /// <summary>
        /// TypeScript object containing stored procedure metadata.
        /// </summary>
        public string PROCS_DEFINITION { get => tokens[nameof(PROCS_DEFINITION)]; init => tokens[nameof(PROCS_DEFINITION)] = value; }
        /// <summary>
        /// Assignment statement for procedure definitions.
        /// </summary>
        public string ENTITY_PROCS_ASSIGNMENT { get => tokens[nameof(ENTITY_PROCS_ASSIGNMENT)]; init => tokens[nameof(ENTITY_PROCS_ASSIGNMENT)] = value; }
    }
}

