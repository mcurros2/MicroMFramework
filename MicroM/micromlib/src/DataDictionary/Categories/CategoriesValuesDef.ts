import { DefaultColumns, EntityColumn, EntityDefinition, CommonFlags as c } from "../../Entity";
import { MicroMClient, ValuesObject } from "../../client";
import { Categories } from "./Categories";


const columns = () =>
(
    {
        c_category_id: new EntityColumn<string>({ name: 'c_category_id', type: 'char', length: 20, flags: c.PK, prompt: 'Categoría', excludeInAutoForm: true }),
        c_categoryvalue_id: new EntityColumn<string>({ name: 'c_categoryvalue_id', type: 'char', length: 20, flags: c.PK, prompt: 'ID' }),
        vc_description: new EntityColumn<string>({ name: 'vc_description', type: 'varchar', length: 255, flags: c.Edit, prompt: 'Descripción' }),
        ...DefaultColumns()
    }
)

const lookups = () =>
(
    {
        Categories: { name: 'Categories', entityConstructor: (client: MicroMClient, parentKeys?: ValuesObject) => new Categories(client, parentKeys) }
    }
)

const views = () =>
(
    {
        cav_brwStandard: { name: 'cav_brwStandard', keyMappings: { c_categoryvalue_id: 0 } }
    }
)
export class CategoriesValuesDef extends EntityDefinition {

    columns = columns();
    views = views();
    lookups = lookups();

    constructor() {
        super('CategoriesValues');
    }

}