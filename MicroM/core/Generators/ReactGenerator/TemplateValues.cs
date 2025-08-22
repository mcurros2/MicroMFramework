namespace MicroM.Generators.ReactGenerator
{
    /// <summary>
    /// Token values used for replacing placeholders in React generator templates.
    /// </summary>
    internal class TemplateValues : TemplateValuesBase
    {
        /// <summary>
        /// Default npm package name for the MicroM library.
        /// </summary>
        public const string CONST_MICROM_LIB_PACKAGE = "@mcurros2/microm";
        /// <summary>
        /// Tokens used when importing lookup utilities.
        /// </summary>
        public const string CONST_ENTITY_LOOKUP_IMPORT = ", MicroMClient, ValuesObject";
        /// <summary>
        /// Statement assigning lookup definitions within a template.
        /// </summary>
        public const string CONST_LOOKUPS_ASSIGNMENT = "lookups = lookups();";
        /// <summary>
        /// Statement assigning procedure definitions within a template.
        /// </summary>
        public const string CONST_PROCS_ASSIGNMENT = "procs = procs();";

        /// <summary>Token for the MNEO identifier.</summary>
        public string MNEO { get => tokens[nameof(MNEO)]; init => tokens[nameof(MNEO)] = value; }
        /// <summary>Token for column definitions.</summary>
        public string COLUMNS_DEFINITION { get => tokens[nameof(COLUMNS_DEFINITION)]; init => tokens[nameof(COLUMNS_DEFINITION)] = value; }
        /// <summary>Token for view key mappings.</summary>
        public string VIEW_KEY_MAPPINGS { get => tokens[nameof(VIEW_KEY_MAPPINGS)]; init => tokens[nameof(VIEW_KEY_MAPPINGS)] = value; }
        /// <summary>Token for lookup definitions within an entity.</summary>
        public string ENTITY_LOOKUPS_DEFINITION { get => tokens[nameof(ENTITY_LOOKUPS_DEFINITION)]; init => tokens[nameof(ENTITY_LOOKUPS_DEFINITION)] = value; }
        /// <summary>Token for the entity class name.</summary>
        public string ENTITY_CLASSNAME { get => tokens[nameof(ENTITY_CLASSNAME)]; init => tokens[nameof(ENTITY_CLASSNAME)] = value; }
        /// <summary>Token for assigning lookup definitions to an entity.</summary>
        public string ENTITY_LOOKUPS_ASSIGNMENT { get => tokens[nameof(ENTITY_LOOKUPS_ASSIGNMENT)]; init => tokens[nameof(ENTITY_LOOKUPS_ASSIGNMENT)] = value; }
        /// <summary>Token for embedding a form component.</summary>
        public string ENTITY_FORM { get => tokens[nameof(ENTITY_FORM)]; init => tokens[nameof(ENTITY_FORM)] = value; }
        /// <summary>Token for additional lookup imports.</summary>
        public string ENTITY_LOOKUP_IMPORT { get => tokens[nameof(ENTITY_LOOKUP_IMPORT)]; init => tokens[nameof(ENTITY_LOOKUP_IMPORT)] = value; }
        /// <summary>Token for the views definition block.</summary>
        public string VIEWS_DEFINITION { get => tokens[nameof(VIEWS_DEFINITION)]; init => tokens[nameof(VIEWS_DEFINITION)] = value; }
        /// <summary>Token for a procedure name.</summary>
        public string PROC_NAME { get => tokens[nameof(PROC_NAME)]; init => tokens[nameof(PROC_NAME)] = value; }
        /// <summary>Token for lookup definitions.</summary>
        public string LOOKUPS_DEFINITION { get => tokens[nameof(LOOKUPS_DEFINITION)]; init => tokens[nameof(LOOKUPS_DEFINITION)] = value; }
        /// <summary>Token for the MicroM library package name.</summary>
        public string MICROM_LIB_PACKAGE { get => tokens[nameof(MICROM_LIB_PACKAGE)]; init => tokens[nameof(MICROM_LIB_PACKAGE)] = value; }
        /// <summary>Token for embedded category imports.</summary>
        public string EMBEDDED_CATEGORIES_IMPORT { get => tokens[nameof(EMBEDDED_CATEGORIES_IMPORT)]; init => tokens[nameof(EMBEDDED_CATEGORIES_IMPORT)] = value; }
        /// <summary>Token for the category identifier.</summary>
        public string CATEGORY_ID { get => tokens[nameof(CATEGORY_ID)]; init => tokens[nameof(CATEGORY_ID)] = value; }
        /// <summary>Token for related category entity definitions.</summary>
        public string RELATED_CATEGORIES_ENTITIES { get => tokens[nameof(RELATED_CATEGORIES_ENTITIES)]; init => tokens[nameof(RELATED_CATEGORIES_ENTITIES)] = value; }
        /// <summary>Token for the category title.</summary>
        public string CATEGORY_TITLE { get => tokens[nameof(CATEGORY_TITLE)]; init => tokens[nameof(CATEGORY_TITLE)] = value; }
        /// <summary>Token listing field components to import.</summary>
        public string FIELDS_IMPORT { get => tokens[nameof(FIELDS_IMPORT)]; init => tokens[nameof(FIELDS_IMPORT)] = value; }
        /// <summary>Token containing JSX for form controls.</summary>
        public string FIELDS_CONTROLS { get => tokens[nameof(FIELDS_CONTROLS)]; init => tokens[nameof(FIELDS_CONTROLS)] = value; }
        /// <summary>Token for the entity title text.</summary>
        public string ENTITY_TITLE { get => tokens[nameof(ENTITY_TITLE)]; init => tokens[nameof(ENTITY_TITLE)] = value; }
        /// <summary>Token for procedure definitions assigned to an entity.</summary>
        public string ENTITY_PROC_DEFINITIONS { get => tokens[nameof(ENTITY_PROC_DEFINITIONS)]; init => tokens[nameof(ENTITY_PROC_DEFINITIONS)] = value; }
        /// <summary>Token for the procedures definition block.</summary>
        public string PROCS_DEFINITION { get => tokens[nameof(PROCS_DEFINITION)]; init => tokens[nameof(PROCS_DEFINITION)] = value; }
        /// <summary>Token assigning procedure definitions to an entity.</summary>
        public string ENTITY_PROCS_ASSIGNMENT { get => tokens[nameof(ENTITY_PROCS_ASSIGNMENT)]; init => tokens[nameof(ENTITY_PROCS_ASSIGNMENT)] = value; }

    }
}
