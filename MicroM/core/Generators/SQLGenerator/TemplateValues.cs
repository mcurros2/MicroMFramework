namespace MicroM.Generators.SQLGenerator
{
    /// <summary>
    /// Provides strongly typed access to tokens used within SQL templates.
    /// Each property exposes a placeholder replaced during generation.
    /// </summary>
    internal class TemplateValues : TemplateValuesBase
    {
        /// <summary>
        /// Token representing the MNEO identifier used in templates.
        /// </summary>
        public string MNEO { get => tokens[nameof(MNEO)]; init => tokens[nameof(MNEO)] = value; }

        /// <summary>
        /// Token for parameter declarations within generated SQL.
        /// </summary>
        public string PARMS_DECLARATION { get => tokens[nameof(PARMS_DECLARATION)]; init => tokens[nameof(PARMS_DECLARATION)] = value; }

        /// <summary>
        /// Token used to declare auto-number variables.
        /// </summary>
        public string AUTONUM_DECLARE { get => tokens[nameof(AUTONUM_DECLARE)]; init => tokens[nameof(AUTONUM_DECLARE)] = value; }

        /// <summary>
        /// Token that holds the table name placeholder.
        /// </summary>
        public string TABLE_NAME { get => tokens[nameof(TABLE_NAME)]; init => tokens[nameof(TABLE_NAME)] = value; }

        /// <summary>
        /// Token containing the WHERE clause for filtering.
        /// </summary>
        public string WHERE_CLAUSE { get => tokens[nameof(WHERE_CLAUSE)]; init => tokens[nameof(WHERE_CLAUSE)] = value; }

        /// <summary>
        /// Token with comma-separated values for INSERT statements.
        /// </summary>
        public string INSERT_VALUES { get => tokens[nameof(INSERT_VALUES)]; init => tokens[nameof(INSERT_VALUES)] = value; }

        /// <summary>
        /// Token with column assignments for UPDATE statements.
        /// </summary>
        public string UPDATE_VALUES { get => tokens[nameof(UPDATE_VALUES)]; init => tokens[nameof(UPDATE_VALUES)] = value; }

        /// <summary>
        /// Token enumerating columns retrieved in GET statements.
        /// </summary>
        public string GET_VALUES { get => tokens[nameof(GET_VALUES)]; init => tokens[nameof(GET_VALUES)] = value; }

        /// <summary>
        /// Token identifying the column used for descriptions.
        /// </summary>
        public string DESC_COLUMN { get => tokens[nameof(DESC_COLUMN)]; init => tokens[nameof(DESC_COLUMN)] = value; }

        /// <summary>
        /// Token listing columns included in view definitions.
        /// </summary>
        public string VIEW_COLUMNS { get => tokens[nameof(VIEW_COLUMNS)]; init => tokens[nameof(VIEW_COLUMNS)] = value; }

        /// <summary>
        /// Token that toggles between CREATE and CREATE OR ALTER statements.
        /// </summary>
        public string CREATE { get => tokens[nameof(CREATE)]; private set => tokens[nameof(CREATE)] = value; }

        /// <summary>
        /// Token for the auto-number column used by templates.
        /// </summary>
        public string AUTONUM { get => tokens[nameof(AUTONUM)]; init => tokens[nameof(AUTONUM)] = value; }

        /// <summary>
        /// Token representing the categories table name.
        /// </summary>
        public string CATEGORIES_TABLE { get => tokens[nameof(CATEGORIES_TABLE)]; init => tokens[nameof(CATEGORIES_TABLE)] = value; }

        /// <summary>
        /// Token representing the status table name.
        /// </summary>
        public string STATUS_TABLE { get => tokens[nameof(STATUS_TABLE)]; init => tokens[nameof(STATUS_TABLE)] = value; }

        /// <summary>
        /// Token containing SQL for updating categories.
        /// </summary>
        public string CATEGORIES_UPDATE { get => tokens[nameof(CATEGORIES_UPDATE)]; init => tokens[nameof(CATEGORIES_UPDATE)] = value; }

        /// <summary>
        /// Token containing SQL for inserting categories.
        /// </summary>
        public string CATEGORIES_INSERT { get => tokens[nameof(CATEGORIES_INSERT)]; init => tokens[nameof(CATEGORIES_INSERT)] = value; }

        /// <summary>
        /// Token with join clauses for category relationships.
        /// </summary>
        public string CATEGORIES_JOIN { get => tokens[nameof(CATEGORIES_JOIN)]; init => tokens[nameof(CATEGORIES_JOIN)] = value; }

        /// <summary>
        /// Token defining delete statements for categories.
        /// </summary>
        public string CATEGORIES_DELETE { get => tokens[nameof(CATEGORIES_DELETE)]; init => tokens[nameof(CATEGORIES_DELETE)] = value; }

        /// <summary>
        /// Token providing insert logic for statuses.
        /// </summary>
        public string STATUS_INSERT { get => tokens[nameof(STATUS_INSERT)]; init => tokens[nameof(STATUS_INSERT)] = value; }

        /// <summary>
        /// Token providing delete logic for statuses.
        /// </summary>
        public string STATUS_DELETE { get => tokens[nameof(STATUS_DELETE)]; init => tokens[nameof(STATUS_DELETE)] = value; }

        /// <summary>
        /// Token providing update logic for statuses.
        /// </summary>
        public string STATUS_UPDATE { get => tokens[nameof(STATUS_UPDATE)]; init => tokens[nameof(STATUS_UPDATE)] = value; }

        /// <summary>
        /// Token for the status parameter placeholder.
        /// </summary>
        public string STATUS_PARM { get => tokens[nameof(STATUS_PARM)]; init => tokens[nameof(STATUS_PARM)] = value; }

        /// <summary>
        /// Token containing SQL that returns newly generated auto-numbers.
        /// </summary>
        public string AUTONUM_RETURN { get => tokens[nameof(AUTONUM_RETURN)]; init => tokens[nameof(AUTONUM_RETURN)] = value; }

        /// <summary>
        /// Token specifying parameter placeholders.
        /// </summary>
        public string PARMS { get => tokens[nameof(PARMS)]; init => tokens[nameof(PARMS)] = value; }

        /// <summary>
        /// Token with validation logic for parameters.
        /// </summary>
        public string PARMS_VALIDATION { get => tokens[nameof(PARMS_VALIDATION)]; init => tokens[nameof(PARMS_VALIDATION)] = value; }

        /// <summary>
        /// Token containing the UPDATE clause body.
        /// </summary>
        public string UPDATE_CLAUSE { get => tokens[nameof(UPDATE_CLAUSE)]; init => tokens[nameof(UPDATE_CLAUSE)] = value; }

        /// <summary>
        /// Token used for last-update control columns.
        /// </summary>
        public string UPDATE_LU_CONTROL { get => tokens[nameof(UPDATE_LU_CONTROL)]; init => tokens[nameof(UPDATE_LU_CONTROL)] = value; }

        /// <summary>
        /// Token for the category parameter placeholder.
        /// </summary>
        public string CATEGORY_PARM { get => tokens[nameof(CATEGORY_PARM)]; init => tokens[nameof(CATEGORY_PARM)] = value; }

        /// <summary>
        /// Token representing null-check logic for category deletes.
        /// </summary>
        public string CATEGORY_DELETE_NULL { get => tokens[nameof(CATEGORY_DELETE_NULL)]; init => tokens[nameof(CATEGORY_DELETE_NULL)] = value; }

        /// <summary>
        /// Token containing JSON for category structures.
        /// </summary>
        public string JSON_CATEGORIES { get => tokens[nameof(JSON_CATEGORIES)]; init => tokens[nameof(JSON_CATEGORIES)] = value; }

        /// <summary>
        /// Token for the current category placeholder.
        /// </summary>
        public string CATEGORY { get => tokens[nameof(CATEGORY)]; init => tokens[nameof(CATEGORY)] = value; }

        /// <summary>
        /// Token naming the temporary table used for categories.
        /// </summary>
        public string CATEGORY_TEMP_TABLE { get => tokens[nameof(CATEGORY_TEMP_TABLE)]; init => tokens[nameof(CATEGORY_TEMP_TABLE)] = value; }

        /// <summary>
        /// Token containing JSON insert statements for categories.
        /// </summary>
        public string JSON_CATEGORIES_INSERT { get => tokens[nameof(JSON_CATEGORIES_INSERT)]; init => tokens[nameof(JSON_CATEGORIES_INSERT)] = value; }

        /// <summary>
        /// Token containing JSON update statements for categories.
        /// </summary>
        public string JSON_CATEGORIES_UPDATE { get => tokens[nameof(JSON_CATEGORIES_UPDATE)]; init => tokens[nameof(JSON_CATEGORIES_UPDATE)] = value; }

        /// <summary>
        /// Token declaring JSON parameters for SQL procedures.
        /// </summary>
        public string JSON_PARMS_DECLARATION { get => tokens[nameof(JSON_PARMS_DECLARATION)]; init => tokens[nameof(JSON_PARMS_DECLARATION)] = value; }

        /// <summary>
        /// Token retrieving categories from JSON collections.
        /// </summary>
        public string JSON_CATEGORIES_GET { get => tokens[nameof(JSON_CATEGORIES_GET)]; init => tokens[nameof(JSON_CATEGORIES_GET)] = value; }

        /// <summary>
        /// Token containing NULLIF checks for validation.
        /// </summary>
        public string NULLIF_CHECKS { get => tokens[nameof(NULLIF_CHECKS)]; init => tokens[nameof(NULLIF_CHECKS)] = value; }

        /// <summary>
        /// Token defining LIKE clause search logic.
        /// </summary>
        public string LIKE_CLAUSE { get => tokens[nameof(LIKE_CLAUSE)]; init => tokens[nameof(LIKE_CLAUSE)] = value; }

        /// <summary>
        /// Token containing a template for building LIKE clauses.
        /// </summary>
        public string LIKE_TEMPLATE { get => tokens[nameof(LIKE_TEMPLATE)]; init => tokens[nameof(LIKE_TEMPLATE)] = value; }

        /// <summary>
        /// Initializes a new instance of <see cref="TemplateValues"/> and sets the
        /// <see cref="CREATE"/> token based on the desired creation mode.
        /// </summary>
        /// <param name="create_or_alter">
        /// When <c>true</c>, the <c>CREATE</c> token is set to "create or alter";
        /// otherwise it is set to "create".
        /// </param>
        public TemplateValues(bool create_or_alter = false) : base()
        {
            CREATE = create_or_alter ? "create or alter" : "create";
        }
    }
}

