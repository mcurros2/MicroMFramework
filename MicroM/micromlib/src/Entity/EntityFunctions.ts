import { Value } from "../client";
import { DefaultColumnsNames } from "./DefaultColumns";
import { Entity } from "./Entity";
import { EntityColumn } from "./EntityColumn";
import { EntityColumnFlags } from "./EntityColumn.types";
import { EntityDefinition } from "./EntityDefinition";


export function getRequiredColumns(entity?: Entity<EntityDefinition>) {
    if(!entity) return [];
    return Object.values(entity.def.columns)
        .filter((col: EntityColumn<Value>) => !col.hasFlag(EntityColumnFlags.nullable) && !col.hasFlag(EntityColumnFlags.autoNum) && !DefaultColumnsNames.includes(col.name))
        .map((col: EntityColumn<Value>) => col.name);
};
