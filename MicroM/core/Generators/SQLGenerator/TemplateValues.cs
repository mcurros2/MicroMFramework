namespace MicroM.Generators.SQLGenerator
{
    /// <summary>
    /// Dictionary of tokens used to replace placeholders within SQL templates.
    /// </summary>
    internal class TemplateValues : TemplateValuesBase
    {
        /// <summary>Placeholder for the entity MNEO name.</summary>
        public string MNEO { get => tokens[nameof(MNEO)]; init => tokens[nameof(MNEO)] = value; }
        /// <summary>Declaration list for procedure parameters.</summary>
        public string PARMS_DECLARATION { get => tokens[nameof(PARMS_DECLARATION)]; init => tokens[nameof(PARMS_DECLARATION)] = value; }
        /// <summary>Declaration for autonumber variables.</summary>
        public string AUTONUM_DECLARE { get => tokens[nameof(AUTONUM_DECLARE)]; init => tokens[nameof(AUTONUM_DECLARE)] = value; }
        /// <summary>Fully qualified table name.</summary>
        public string TABLE_NAME { get => tokens[nameof(TABLE_NAME)]; init => tokens[nameof(TABLE_NAME)] = value; }
        /// <summary>WHERE clause used for filtering.</summary>
        public string WHERE_CLAUSE { get => tokens[nameof(WHERE_CLAUSE)]; init => tokens[nameof(WHERE_CLAUSE)] = value; }
        /// <summary>VALUES list used for INSERT statements.</summary>
        public string INSERT_VALUES { get => tokens[nameof(INSERT_VALUES)]; init => tokens[nameof(INSERT_VALUES)] = value; }
        /// <summary>SET clause used in UPDATE statements.</summary>
        public string UPDATE_VALUES { get => tokens[nameof(UPDATE_VALUES)]; init => tokens[nameof(UPDATE_VALUES)] = value; }
        /// <summary>Column list used for SELECT statements.</summary>
        public string GET_VALUES { get => tokens[nameof(GET_VALUES)]; init => tokens[nameof(GET_VALUES)] = value; }
        /// <summary>Description column used by lookup procedures.</summary>
        public string DESC_COLUMN { get => tokens[nameof(DESC_COLUMN)]; init => tokens[nameof(DESC_COLUMN)] = value; }
        /// <summary>Columns returned by view procedures.</summary>
        public string VIEW_COLUMNS { get => tokens[nameof(VIEW_COLUMNS)]; init => tokens[nameof(VIEW_COLUMNS)] = value; }
        /// <summary><c>create</c> or <c>create or alter</c> keyword.</summary>
        public string CREATE { get => tokens[nameof(CREATE)]; private set => tokens[nameof(CREATE)] = value; }
        /// <summary>Script fragment used to generate autonumbers.</summary>
        public string AUTONUM { get => tokens[nameof(AUTONUM)]; init => tokens[nameof(AUTONUM)] = value; }
        /// <summary>Name of the categories table for the entity.</summary>
        public string CATEGORIES_TABLE { get => tokens[nameof(CATEGORIES_TABLE)]; init => tokens[nameof(CATEGORIES_TABLE)] = value; }
        /// <summary>Name of the status table for the entity.</summary>
        public string STATUS_TABLE { get => tokens[nameof(STATUS_TABLE)]; init => tokens[nameof(STATUS_TABLE)] = value; }
        /// <summary>SQL used to update category rows.</summary>
        public string CATEGORIES_UPDATE { get => tokens[nameof(CATEGORIES_UPDATE)]; init => tokens[nameof(CATEGORIES_UPDATE)] = value; }
        /// <summary>SQL used to insert category rows.</summary>
        public string CATEGORIES_INSERT { get => tokens[nameof(CATEGORIES_INSERT)]; init => tokens[nameof(CATEGORIES_INSERT)] = value; }
        /// <summary>JOIN clause for category tables.</summary>
        public string CATEGORIES_JOIN { get => tokens[nameof(CATEGORIES_JOIN)]; init => tokens[nameof(CATEGORIES_JOIN)] = value; }
        /// <summary>SQL used to delete category rows.</summary>
        public string CATEGORIES_DELETE { get => tokens[nameof(CATEGORIES_DELETE)]; init => tokens[nameof(CATEGORIES_DELETE)] = value; }
        /// <summary>SQL used to insert status rows.</summary>
        public string STATUS_INSERT { get => tokens[nameof(STATUS_INSERT)]; init => tokens[nameof(STATUS_INSERT)] = value; }
        /// <summary>SQL used to delete status rows.</summary>
        public string STATUS_DELETE { get => tokens[nameof(STATUS_DELETE)]; init => tokens[nameof(STATUS_DELETE)] = value; }
        /// <summary>SQL used to update status rows.</summary>
        public string STATUS_UPDATE { get => tokens[nameof(STATUS_UPDATE)]; init => tokens[nameof(STATUS_UPDATE)] = value; }
        /// <summary>Parameter name representing a status value.</summary>
        public string STATUS_PARM { get => tokens[nameof(STATUS_PARM)]; init => tokens[nameof(STATUS_PARM)] = value; }
        /// <summary>Return statement for autonumber values.</summary>
        public string AUTONUM_RETURN { get => tokens[nameof(AUTONUM_RETURN)]; init => tokens[nameof(AUTONUM_RETURN)] = value; }
        /// <summary>Comma separated parameter list.</summary>
        public string PARMS { get => tokens[nameof(PARMS)]; init => tokens[nameof(PARMS)] = value; }
        /// <summary>Validation block for input parameters.</summary>
        public string PARMS_VALIDATION { get => tokens[nameof(PARMS_VALIDATION)]; init => tokens[nameof(PARMS_VALIDATION)] = value; }
        /// <summary>UPDATE clause for the main table.</summary>
        public string UPDATE_CLAUSE { get => tokens[nameof(UPDATE_CLAUSE)]; init => tokens[nameof(UPDATE_CLAUSE)] = value; }
        /// <summary>Optimistic locking control segment.</summary>
        public string UPDATE_LU_CONTROL { get => tokens[nameof(UPDATE_LU_CONTROL)]; init => tokens[nameof(UPDATE_LU_CONTROL)] = value; }
        /// <summary>Parameter name for category identifiers.</summary>
        public string CATEGORY_PARM { get => tokens[nameof(CATEGORY_PARM)]; init => tokens[nameof(CATEGORY_PARM)] = value; }
        /// <summary>Conditional delete clause when category parameter is null.</summary>
        public string CATEGORY_DELETE_NULL { get => tokens[nameof(CATEGORY_DELETE_NULL)]; init => tokens[nameof(CATEGORY_DELETE_NULL)] = value; }
        /// <summary>Fragment that prepares JSON category arrays.</summary>
        public string JSON_CATEGORIES { get => tokens[nameof(JSON_CATEGORIES)]; init => tokens[nameof(JSON_CATEGORIES)] = value; }
        /// <summary>Identifier of a category.</summary>
        public string CATEGORY { get => tokens[nameof(CATEGORY)]; init => tokens[nameof(CATEGORY)] = value; }
        /// <summary>Temporary table name for parsed categories.</summary>
        public string CATEGORY_TEMP_TABLE { get => tokens[nameof(CATEGORY_TEMP_TABLE)]; init => tokens[nameof(CATEGORY_TEMP_TABLE)] = value; }
        /// <summary>SQL used to insert parsed JSON categories.</summary>
        public string JSON_CATEGORIES_INSERT { get => tokens[nameof(JSON_CATEGORIES_INSERT)]; init => tokens[nameof(JSON_CATEGORIES_INSERT)] = value; }
        /// <summary>SQL used to update categories from JSON arrays.</summary>
        public string JSON_CATEGORIES_UPDATE { get => tokens[nameof(JSON_CATEGORIES_UPDATE)]; init => tokens[nameof(JSON_CATEGORIES_UPDATE)] = value; }
        /// <summary>Declarations for variables used to hold JSON arrays.</summary>
        public string JSON_PARMS_DECLARATION { get => tokens[nameof(JSON_PARMS_DECLARATION)]; init => tokens[nameof(JSON_PARMS_DECLARATION)] = value; }
        /// <summary>SQL used to fetch category values as JSON.</summary>
        public string JSON_CATEGORIES_GET { get => tokens[nameof(JSON_CATEGORIES_GET)]; init => tokens[nameof(JSON_CATEGORIES_GET)] = value; }
        /// <summary>Statements that convert empty strings to NULL.</summary>
        public string NULLIF_CHECKS { get => tokens[nameof(NULLIF_CHECKS)]; init => tokens[nameof(NULLIF_CHECKS)] = value; }
        /// <summary>LIKE clause used in view filters.</summary>
        public string LIKE_CLAUSE { get => tokens[nameof(LIKE_CLAUSE)]; init => tokens[nameof(LIKE_CLAUSE)] = value; }
        /// <summary>Template fragment for the LIKE filter.</summary>
        public string LIKE_TEMPLATE { get => tokens[nameof(LIKE_TEMPLATE)]; init => tokens[nameof(LIKE_TEMPLATE)] = value; }

        public TemplateValues(bool create_or_alter = false) : base()
        {
            CREATE = create_or_alter ? "create or alter" : "create";
        }

    }

}
