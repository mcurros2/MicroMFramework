import { SQLType, Value } from "../client";

/** Flags for determining the role of the column */
export enum EntityColumnFlags {
    /** This flag indicates that this column is NOT sent as parameter to any API endpoint */
    None = 0,
    /**Flag: get. This column will be sent as parameter for the GET API endpoint */
    get = 1 << 0,
    /**Flag: add. This column will be sent as parameter for the ADD API endpoint */
    add = 1 << 1,
    /**Flag: edit. This column will be sent as parameter for the EDIT API endpoint */
    edit = 1 << 2,
    /**Flag: delete. This column will be sent as parameter for the DELETE API endpoint */
    delete = 1 << 3,
    /**Flag: pk. This column is part of the PRIMARY KEY and will be sent as parameter for any API endpoint that requires a key */
    pk = 1 << 4,
    /**Flag: fk. This column is part of a FOREIGN KEY and will be sent as parameter for any API endpoint that requires a PARENT KEYS */
    fk = 1 << 5,
    /**Flag: autoNum. This column will have an incremental number assigned to it as result of calling the ADD API */
    autoNum = 1 << 6,
    /**Flag: nullable. This column accpets NULL values at the SQL table level */
    nullable = 1 << 7,
    /**Flag: nullable. This column is not part of the physical entity, it may be used as a calculated value or embedded value from other entity */
    fake = 1 << 8
}

export interface EntityColumnOptions<T extends Value> {
    name: string,
    type: SQLType,
    length?: number,
    scale?: number,
    value?: T,
    defaultValue?: T,
    flags: EntityColumnFlags,
    prompt?: string,
    placeholder?: string,
    description?: string,
    isArray?: boolean,
    excludeInAutoForm?: boolean
    valueDescription?: string
}

/** CommonFlags are pre assembled EntityColumnFlags to ease declaring columns */
export const CommonFlags: {
    /** PK = EntityColumnFlags.add | EntityColumnFlags.edit | EntityColumnFlags.delete | EntityColumnFlags.pk*/
    PK: EntityColumnFlags,
    /**PKAutonum = EntityColumnFlags.add | EntityColumnFlags.edit | EntityColumnFlags.delete | EntityColumnFlags.pk | EntityColumnFlags.autoNum*/
    PKAutonum: EntityColumnFlags,
    /**FK = EntityColumnFlags.add | EntityColumnFlags.edit | EntityColumnFlags.fk*/
    FK: EntityColumnFlags,
    /**Edit = EntityColumnFlags.add | EntityColumnFlags.edit*/
    Edit: EntityColumnFlags
} = {
    PK: EntityColumnFlags.get | EntityColumnFlags.add | EntityColumnFlags.edit | EntityColumnFlags.delete | EntityColumnFlags.pk,
    PKAutonum: EntityColumnFlags.get | EntityColumnFlags.add | EntityColumnFlags.edit | EntityColumnFlags.delete | EntityColumnFlags.pk | EntityColumnFlags.autoNum,
    FK: EntityColumnFlags.add | EntityColumnFlags.edit | EntityColumnFlags.delete | EntityColumnFlags.get | EntityColumnFlags.fk,
    Edit: EntityColumnFlags.add | EntityColumnFlags.edit | EntityColumnFlags.get | EntityColumnFlags.delete
}

export interface ColumnsFilter {
    flags?: EntityColumnFlags,
    ignoreDefaults?: boolean,
    matchAllFlags?: boolean,
    ignoreSystemColumns?: boolean
}


