import { MicroMClient, ValuesObject } from "../client";
import { Entity } from "./Entity";
import { EntityDefinition } from "./EntityDefinition";

export interface ViewMapping {
    keyIndex: number,
    descriptionIndex: number
}

export interface EntityConstructor {
    (client: MicroMClient, parentKeys?: ValuesObject): Entity<EntityDefinition>;
}

export interface EntityLookup {
    name: string;
    entityConstructor: EntityConstructor;
    view?: string;
    proc?: string;
    bindingColumnKey?: string;
    viewMapping?: ViewMapping;
}
