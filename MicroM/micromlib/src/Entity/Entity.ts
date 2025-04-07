import { IconProps } from "@tabler/icons-react";
import { ComponentType } from "react";
import { FormOptions } from "../UI/Core/types";
import { MicroMClient } from "../client/MicromClient";
import { ValuesObject } from "../client/client.types";
import { EntityAPI } from "./EntityAPI";
import { EntityDefinition } from "./EntityDefinition";

export type EntityFormComponentPromise = Promise<ComponentType<FormOptions<Entity<any>>>>;

export class Entity<T extends EntityDefinition> {
    #def: T;

    get name() { return this.#def.name; }

    get def() { return this.#def }

    parentKeys;

    API;

    Form: EntityFormComponentPromise | "AutoForm" | "AutoFiltersForm" | null;

    Title: string;

    HelpText?: string;

    Icon?: ComponentType<IconProps>;

    constructor(client: MicroMClient, def: T, parentKeys: ValuesObject = {}) {
        this.#def = def;
        this.parentKeys = parentKeys;
        this.API = new EntityAPI(client, this.#def.name, this.#def.columns);
        this.Form = null;
        this.Title = this.def.name;
    };

    static clone(entity: Entity<EntityDefinition>) {
        const new_def = EntityDefinition.clone(entity.def);
        const new_parentkeys: ValuesObject = {};
        for (const key in entity.parentKeys) {
            const source = entity.parentKeys[key];
            // MMC: you may have a null value as column in parentKeys, so we need to check for it.
            if (source) new_parentkeys[key] = source;
        }
        const new_entity = new Entity(entity.API.client, new_def, new_parentkeys);
        new_entity.Form = entity.Form;
        new_entity.Title = entity.Title;
        new_entity.HelpText = entity.HelpText;
        new_entity.Icon = entity.Icon;
        return new_entity;
    }

}