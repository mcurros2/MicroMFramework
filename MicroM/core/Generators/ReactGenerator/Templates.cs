namespace MicroM.Generators.ReactGenerator
{
    /// <summary>
    /// Provides string templates used by the React generator.
    /// </summary>
    internal class Templates
    {
        /// <summary>
        /// Template used for generating a category entity.
        /// </summary>
        internal const string CATEGORY_ENTITY_TEMPLATE =
@"
import { CategoriesValues, MicroMClient } from ""{MICROM_LIB_PACKAGE}"";
import { IconCategory } from ""@tabler/icons-react"";

export const {CATEGORY_ID}Icon = IconCategory;
export const {CATEGORY_ID}HelpText = ""* <Your help text>"";

export class cat{CATEGORY_ID} extends CategoriesValues {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, parentKeys);
        this.def.columns.c_category_id.value = ""{CATEGORY_ID}"";
        this.def.columns.c_category_id.defaultValue = ""{CATEGORY_ID}"";
        this.Title = ""{CATEGORY_TITLE}"";
        this.HelpText = {CATEGORY_ID}HelpText;
        this.Icon = {CATEGORY_ID}Icon;
    }
}

";

        /// <summary>
        /// Template that defines the structure of entity lookups.
        /// </summary>
        internal const string ENTITY_LOOKUPS_TEMPLATE =
@"
const lookups = () =>
(
    {LOOKUPS_DEFINITION}
)
";

        /// <summary>
        /// Template that defines entity procedure declarations.
        /// </summary>
        internal const string ENTITY_PROCS_TEMPLATE =
@"
const procs = () =>
(
    {PROCS_DEFINITION}
)
";

        /// <summary>
        /// Template for generating the entity definition.
        /// </summary>
        internal const string ENTITY_DEFINITION_TEMPLATE =
@"
import { DefaultColumns, EntityColumn, EntityDefinition{ENTITY_LOOKUP_IMPORT}, EntityColumnFlags, CommonFlags as c } from ""{MICROM_LIB_PACKAGE}"";
import { {EMBEDDED_CATEGORIES_IMPORT} } from ""./Categories"";

const columns = () =>
(
    {
        {COLUMNS_DEFINITION},
        ...DefaultColumns()
    }
)

const views = () =>
(
    {VIEWS_DEFINITION}
)

{ENTITY_LOOKUPS_DEFINITION}
{ENTITY_PROC_DEFINITIONS}
export class {ENTITY_CLASSNAME}Def extends EntityDefinition {

    columns = columns();
    views = views();
    {ENTITY_LOOKUPS_ASSIGNMENT}
    {ENTITY_PROCS_ASSIGNMENT}

    constructor() {
        super('{ENTITY_CLASSNAME}');
    }
}
";

        /// <summary>
        /// Template for generating the entity class.
        /// </summary>
        internal const string ENTITY_TEMPLATE =
@"
import { Entity, MicroMClient } from ""{MICROM_LIB_PACKAGE}"";
import { {ENTITY_CLASSNAME}Def } from ""./{ENTITY_CLASSNAME}Def"";

export const {ENTITY_CLASSNAME}Icon = Your_Icon ;
export const {ENTITY_CLASSNAME}HelpText = '* <Your help text>.';

export class {ENTITY_CLASSNAME} extends Entity<{ENTITY_CLASSNAME}Def> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new {ENTITY_CLASSNAME}Def(), parentKeys);
        this.Form = import('./{ENTITY_CLASSNAME}Form').then((module) => module.{ENTITY_CLASSNAME}Form);
        this.Title = ""{ENTITY_TITLE}"";
        this.HelpText = {ENTITY_CLASSNAME}HelpText;
        this.Icon = {ENTITY_CLASSNAME}Icon;
    }
}
";

        /// <summary>
        /// Template for generating the entity form component.
        /// </summary>
        internal const string ENTITY_FORM_TEMPLATE =
@"
import { Stack, useComponentDefaultProps, useMantineTheme } from ""@mantine/core"";
import { EntityForm, FormOptions, useEntityForm, {FIELDS_IMPORT} } from ""@mcurros2/microm"";
import { {ENTITY_CLASSNAME} } from ""./{ENTITY_CLASSNAME}"";


export const {ENTITY_CLASSNAME}FormDefaultProps: Partial<FormOptions<{ENTITY_CLASSNAME}>> = {
    initialFormMode: ""view""
}


export function {ENTITY_CLASSNAME}Form(props: FormOptions<{ENTITY_CLASSNAME}>) {

    const { entity, initialFormMode, getDataOnInit, onSaved, onCancel } = useComponentDefaultProps('{ENTITY_CLASSNAME}', {ENTITY_CLASSNAME}FormDefaultProps, props);
    const formAPI = useEntityForm({ entity: entity, initialFormMode, getDataOnInit: getDataOnInit!, onSaved, onCancel });
    const { formMode, status } = formAPI;

    const theme = useMantineTheme();

    return (
        <EntityForm formAPI={formAPI}>
            <Stack>
                {FIELDS_CONTROLS}
            </Stack>
        </EntityForm>
    )
}
";


    }
}
