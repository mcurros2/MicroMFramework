import { useMemo, useRef } from "react";
import { ValuesObject } from "../../client";
import { Entity, EntityDefinition, EntityLookup } from "../../Entity";

export interface UseLookupEntityOptions {
    parentKeys?: ValuesObject
    entity: Entity<EntityDefinition>,
    lookupDefName: string,
}

export function useLookupEntity({ entity, lookupDefName, parentKeys }: UseLookupEntityOptions) {
    const lookupEntity = useRef<Entity<EntityDefinition>>();
    const lookupDef = useRef<EntityLookup>();
    const viewName = useRef<string>('');

    useMemo(() => {
        // MMC: create the lookup entity 
        lookupDef.current = entity.def.lookups[lookupDefName];
        lookupEntity.current = lookupDef.current.entityConstructor(entity.API.client, parentKeys);

        const stdview = lookupEntity.current.def.standardView() ?? '';
        viewName.current = lookupDef.current.view ? lookupDef.current.view : stdview;

    }, [entity, lookupDefName, parentKeys]);

    return {
        lookupEntity: lookupEntity.current,
        lookupDef: lookupDef.current,
        lookupViewName: viewName.current
    }
}