import { useEffect, useMemo, useState } from "react";
import { Entity, EntityDefinition } from "../../Entity";
import { EntityConstructor } from "../../Entity/EntityLookup";
import { MicroMClient } from "../../client/MicromClient";
import { ValuesObject } from "../../client/client.types";
import { EntitySourceProps } from "./GetEntity";

export interface UseResolvedEntityConstructorResult {
    entityConstructor: EntityConstructor | null;
    loading: boolean;
    error: unknown | null;
}

interface AsyncEntityState {
    entity: Entity<EntityDefinition> | null;
    error: unknown | null;
}

const EMPTY_ASYNC_STATE: AsyncEntityState = { entity: null, error: null };

export function useResolvedEntityConstructor(client: MicroMClient, parentKeys: ValuesObject | undefined, source: EntitySourceProps): UseResolvedEntityConstructorResult {
    const { entityConstructor, entityLoader } = source;

    const syncConstructor = useMemo<EntityConstructor | null>(
        () => entityConstructor ? (c: MicroMClient) => entityConstructor(c, parentKeys) : null,
        [entityConstructor, parentKeys]
    );

    const [asyncState, setAsyncState] = useState<AsyncEntityState>(EMPTY_ASYNC_STATE);

    useEffect(() => {
        if (syncConstructor || !entityLoader) return;

        let mounted = true;
        setAsyncState(EMPTY_ASYNC_STATE);

        entityLoader(client, parentKeys)
            .then(entity => { if (mounted) setAsyncState({ entity, error: null }); })
            .catch(error => { if (mounted) setAsyncState({ entity: null, error }); });

        return () => { mounted = false; };
    }, [syncConstructor, entityLoader, client, parentKeys]);

    if (syncConstructor) return { entityConstructor: syncConstructor, loading: false, error: null };

    if (asyncState.error) return { entityConstructor: null, loading: false, error: asyncState.error };

    if (asyncState.entity) {
        const entity = asyncState.entity;
        return { entityConstructor: () => entity, loading: false, error: null };
    }

    return { entityConstructor: null, loading: true, error: null };
}
