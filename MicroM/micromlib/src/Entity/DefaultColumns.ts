import { EntityColumn } from "./EntityColumn";
import { CommonFlags as c, EntityColumnFlags as f } from "./EntityColumn.types";
import { ColumnsObject } from "./EntityColumnCollection.types";

export const SYSTEM_COLUMNS_NAMES = ['dt_inserttime', 'dt_lu', 'vc_webinsuser', 'vc_webluuser', 'vc_insuser', 'vc_luuser'];

export interface IDefaultColumns extends ColumnsObject {
    dt_inserttime: EntityColumn<Date>,
    dt_lu: EntityColumn<Date>,
    vc_webinsuser: EntityColumn<string>,
    vc_webluuser: EntityColumn<string>,
    vc_insuser: EntityColumn<string>,
    vc_luuser: EntityColumn<string>,
}

export const DefaultColumnsNames = ['dt_inserttime', 'dt_lu', 'vc_webinsuser', 'vc_webluuser', 'vc_insuser', 'vc_luuser'];

export const DefaultColumns = (): IDefaultColumns =>
(
    {
        dt_inserttime: new EntityColumn<Date>({ name: 'dt_inserttime', type: 'datetime', flags: f.None }),
        dt_lu: new EntityColumn<Date>({ name: 'dt_lu', type: 'datetime', flags: c.Edit }),
        vc_webinsuser: new EntityColumn<string>({ name: 'vc_webinsuser', type: 'varchar', length: 255, flags: c.Edit }),
        vc_webluuser: new EntityColumn<string>({ name: 'vc_webluuser', type: 'varchar', length: 255, flags: f.None }),
        vc_insuser: new EntityColumn<string>({ name: 'vc_insuser', type: 'varchar', length: 255, flags: f.None }),
        vc_luuser: new EntityColumn<string>({ name: 'vc_luuser', type: 'varchar', length: 255, flags: f.None }),
    }
)