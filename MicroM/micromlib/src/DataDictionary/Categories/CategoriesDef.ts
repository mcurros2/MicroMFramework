import { DefaultColumns, EntityColumn, EntityDefinition, CommonFlags as c } from "../../Entity";

const columns = () =>
(
    {
        c_category_id: new EntityColumn<string>({ name: 'c_category_id', type: 'char', length: 20, flags: c.PK, prompt: 'Categoría' }),
        vc_description: new EntityColumn<string>({ name: 'vc_description', type: 'varchar', length: 255, flags: c.Edit, prompt: 'Descripción' }),
        ...DefaultColumns()
    }
)

const views = () =>
(
    {
        cat_brwStandard: { name: 'cat_brwStandard', keyMappings: { c_category_id: 0 } }
    }
)

export class CategoriesDef extends EntityDefinition {

    columns = columns();
    views = views();

    constructor() {
        super('Categories');
    }

}