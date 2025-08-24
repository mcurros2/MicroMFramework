namespace MicroM.Generators.SQLGenerator
{
    internal class TemplateValues : TemplateValuesBase
    {
        public string MNEO { get => tokens[nameof(MNEO)]; init => tokens[nameof(MNEO)] = value; }
        public string PARMS_DECLARATION { get => tokens[nameof(PARMS_DECLARATION)]; init => tokens[nameof(PARMS_DECLARATION)] = value; }
        public string AUTONUM_DECLARE { get => tokens[nameof(AUTONUM_DECLARE)]; init => tokens[nameof(AUTONUM_DECLARE)] = value; }
        public string TABLE_NAME { get => tokens[nameof(TABLE_NAME)]; init => tokens[nameof(TABLE_NAME)] = value; }
        public string WHERE_CLAUSE { get => tokens[nameof(WHERE_CLAUSE)]; init => tokens[nameof(WHERE_CLAUSE)] = value; }
        public string INSERT_VALUES { get => tokens[nameof(INSERT_VALUES)]; init => tokens[nameof(INSERT_VALUES)] = value; }
        public string UPDATE_VALUES { get => tokens[nameof(UPDATE_VALUES)]; init => tokens[nameof(UPDATE_VALUES)] = value; }
        public string GET_VALUES { get => tokens[nameof(GET_VALUES)]; init => tokens[nameof(GET_VALUES)] = value; }
        public string DESC_COLUMN { get => tokens[nameof(DESC_COLUMN)]; init => tokens[nameof(DESC_COLUMN)] = value; }
        public string VIEW_COLUMNS { get => tokens[nameof(VIEW_COLUMNS)]; init => tokens[nameof(VIEW_COLUMNS)] = value; }
        public string CREATE { get => tokens[nameof(CREATE)]; private set => tokens[nameof(CREATE)] = value; }
        public string AUTONUM { get => tokens[nameof(AUTONUM)]; init => tokens[nameof(AUTONUM)] = value; }
        public string CATEGORIES_TABLE { get => tokens[nameof(CATEGORIES_TABLE)]; init => tokens[nameof(CATEGORIES_TABLE)] = value; }
        public string STATUS_TABLE { get => tokens[nameof(STATUS_TABLE)]; init => tokens[nameof(STATUS_TABLE)] = value; }
        public string CATEGORIES_UPDATE { get => tokens[nameof(CATEGORIES_UPDATE)]; init => tokens[nameof(CATEGORIES_UPDATE)] = value; }
        public string CATEGORIES_INSERT { get => tokens[nameof(CATEGORIES_INSERT)]; init => tokens[nameof(CATEGORIES_INSERT)] = value; }
        public string CATEGORIES_JOIN { get => tokens[nameof(CATEGORIES_JOIN)]; init => tokens[nameof(CATEGORIES_JOIN)] = value; }
        public string CATEGORIES_DELETE { get => tokens[nameof(CATEGORIES_DELETE)]; init => tokens[nameof(CATEGORIES_DELETE)] = value; }
        public string STATUS_INSERT { get => tokens[nameof(STATUS_INSERT)]; init => tokens[nameof(STATUS_INSERT)] = value; }
        public string STATUS_DELETE { get => tokens[nameof(STATUS_DELETE)]; init => tokens[nameof(STATUS_DELETE)] = value; }
        public string STATUS_UPDATE { get => tokens[nameof(STATUS_UPDATE)]; init => tokens[nameof(STATUS_UPDATE)] = value; }
        public string STATUS_PARM { get => tokens[nameof(STATUS_PARM)]; init => tokens[nameof(STATUS_PARM)] = value; }
        public string AUTONUM_RETURN { get => tokens[nameof(AUTONUM_RETURN)]; init => tokens[nameof(AUTONUM_RETURN)] = value; }
        public string PARMS { get => tokens[nameof(PARMS)]; init => tokens[nameof(PARMS)] = value; }
        public string PARMS_VALIDATION { get => tokens[nameof(PARMS_VALIDATION)]; init => tokens[nameof(PARMS_VALIDATION)] = value; }
        public string UPDATE_CLAUSE { get => tokens[nameof(UPDATE_CLAUSE)]; init => tokens[nameof(UPDATE_CLAUSE)] = value; }
        public string UPDATE_LU_CONTROL { get => tokens[nameof(UPDATE_LU_CONTROL)]; init => tokens[nameof(UPDATE_LU_CONTROL)] = value; }
        public string CATEGORY_PARM { get => tokens[nameof(CATEGORY_PARM)]; init => tokens[nameof(CATEGORY_PARM)] = value; }
        public string CATEGORY_DELETE_NULL { get => tokens[nameof(CATEGORY_DELETE_NULL)]; init => tokens[nameof(CATEGORY_DELETE_NULL)] = value; }
        public string JSON_CATEGORIES { get => tokens[nameof(JSON_CATEGORIES)]; init => tokens[nameof(JSON_CATEGORIES)] = value; }
        public string CATEGORY { get => tokens[nameof(CATEGORY)]; init => tokens[nameof(CATEGORY)] = value; }
        public string CATEGORY_TEMP_TABLE { get => tokens[nameof(CATEGORY_TEMP_TABLE)]; init => tokens[nameof(CATEGORY_TEMP_TABLE)] = value; }
        public string JSON_CATEGORIES_INSERT { get => tokens[nameof(JSON_CATEGORIES_INSERT)]; init => tokens[nameof(JSON_CATEGORIES_INSERT)] = value; }
        public string JSON_CATEGORIES_UPDATE { get => tokens[nameof(JSON_CATEGORIES_UPDATE)]; init => tokens[nameof(JSON_CATEGORIES_UPDATE)] = value; }
        public string JSON_PARMS_DECLARATION { get => tokens[nameof(JSON_PARMS_DECLARATION)]; init => tokens[nameof(JSON_PARMS_DECLARATION)] = value; }
        public string JSON_CATEGORIES_GET { get => tokens[nameof(JSON_CATEGORIES_GET)]; init => tokens[nameof(JSON_CATEGORIES_GET)] = value; }
        public string NULLIF_CHECKS { get => tokens[nameof(NULLIF_CHECKS)]; init => tokens[nameof(NULLIF_CHECKS)] = value; }
        public string LIKE_CLAUSE { get => tokens[nameof(LIKE_CLAUSE)]; init => tokens[nameof(LIKE_CLAUSE)] = value; }
        public string LIKE_TEMPLATE { get => tokens[nameof(LIKE_TEMPLATE)]; init => tokens[nameof(LIKE_TEMPLATE)] = value; }

        public TemplateValues(bool create_or_alter = false) : base()
        {
            CREATE = create_or_alter ? "create or alter" : "create";
        }

    }

}
