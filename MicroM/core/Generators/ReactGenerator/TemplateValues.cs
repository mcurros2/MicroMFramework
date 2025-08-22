namespace MicroM.Generators.ReactGenerator
{
    internal class TemplateValues : TemplateValuesBase
    {
        public const string CONST_MICROM_LIB_PACKAGE = "@mcurros2/microm";
        public const string CONST_ENTITY_LOOKUP_IMPORT = ", MicroMClient, ValuesObject";
        public const string CONST_LOOKUPS_ASSIGNMENT = "lookups = lookups();";
        public const string CONST_PROCS_ASSIGNMENT = "procs = procs();";

        public string MNEO { get => tokens[nameof(MNEO)]; init => tokens[nameof(MNEO)] = value; }
        public string COLUMNS_DEFINITION { get => tokens[nameof(COLUMNS_DEFINITION)]; init => tokens[nameof(COLUMNS_DEFINITION)] = value; }
        public string VIEW_KEY_MAPPINGS { get => tokens[nameof(VIEW_KEY_MAPPINGS)]; init => tokens[nameof(VIEW_KEY_MAPPINGS)] = value; }
        public string ENTITY_LOOKUPS_DEFINITION { get => tokens[nameof(ENTITY_LOOKUPS_DEFINITION)]; init => tokens[nameof(ENTITY_LOOKUPS_DEFINITION)] = value; }
        public string ENTITY_CLASSNAME { get => tokens[nameof(ENTITY_CLASSNAME)]; init => tokens[nameof(ENTITY_CLASSNAME)] = value; }
        public string ENTITY_LOOKUPS_ASSIGNMENT { get => tokens[nameof(ENTITY_LOOKUPS_ASSIGNMENT)]; init => tokens[nameof(ENTITY_LOOKUPS_ASSIGNMENT)] = value; }
        public string ENTITY_FORM { get => tokens[nameof(ENTITY_FORM)]; init => tokens[nameof(ENTITY_FORM)] = value; }
        public string ENTITY_LOOKUP_IMPORT { get => tokens[nameof(ENTITY_LOOKUP_IMPORT)]; init => tokens[nameof(ENTITY_LOOKUP_IMPORT)] = value; }
        public string VIEWS_DEFINITION { get => tokens[nameof(VIEWS_DEFINITION)]; init => tokens[nameof(VIEWS_DEFINITION)] = value; }
        public string PROC_NAME { get => tokens[nameof(PROC_NAME)]; init => tokens[nameof(PROC_NAME)] = value; }
        public string LOOKUPS_DEFINITION { get => tokens[nameof(LOOKUPS_DEFINITION)]; init => tokens[nameof(LOOKUPS_DEFINITION)] = value; }
        public string MICROM_LIB_PACKAGE { get => tokens[nameof(MICROM_LIB_PACKAGE)]; init => tokens[nameof(MICROM_LIB_PACKAGE)] = value; }
        public string EMBEDDED_CATEGORIES_IMPORT { get => tokens[nameof(EMBEDDED_CATEGORIES_IMPORT)]; init => tokens[nameof(EMBEDDED_CATEGORIES_IMPORT)] = value; }
        public string CATEGORY_ID { get => tokens[nameof(CATEGORY_ID)]; init => tokens[nameof(CATEGORY_ID)] = value; }
        public string RELATED_CATEGORIES_ENTITIES { get => tokens[nameof(RELATED_CATEGORIES_ENTITIES)]; init => tokens[nameof(RELATED_CATEGORIES_ENTITIES)] = value; }
        public string CATEGORY_TITLE { get => tokens[nameof(CATEGORY_TITLE)]; init => tokens[nameof(CATEGORY_TITLE)] = value; }
        public string FIELDS_IMPORT { get => tokens[nameof(FIELDS_IMPORT)]; init => tokens[nameof(FIELDS_IMPORT)] = value; }
        public string FIELDS_CONTROLS { get => tokens[nameof(FIELDS_CONTROLS)]; init => tokens[nameof(FIELDS_CONTROLS)] = value; }
        public string ENTITY_TITLE { get => tokens[nameof(ENTITY_TITLE)]; init => tokens[nameof(ENTITY_TITLE)] = value; }
        public string ENTITY_PROC_DEFINITIONS { get => tokens[nameof(ENTITY_PROC_DEFINITIONS)]; init => tokens[nameof(ENTITY_PROC_DEFINITIONS)] = value; }
        public string PROCS_DEFINITION { get => tokens[nameof(PROCS_DEFINITION)]; init => tokens[nameof(PROCS_DEFINITION)] = value; }
        public string ENTITY_PROCS_ASSIGNMENT { get => tokens[nameof(ENTITY_PROCS_ASSIGNMENT)]; init => tokens[nameof(ENTITY_PROCS_ASSIGNMENT)] = value; }

    }
}
