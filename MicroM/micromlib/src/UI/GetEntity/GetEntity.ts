import { ComponentType } from "react";
import { Entity, EntityDefinition } from "../../Entity";
import { MicroMClient } from "../../client/MicromClient";
import { ValuesObject } from "../../client/client.types";
import { EntityCardProps } from "../EntityCard/EntityCard";

export type EntityBuilderProps = { entity: Entity<EntityDefinition>, view: string, Card: ComponentType<EntityCardProps<ValuesObject>> }

export type EntityGridBuilderProps = Omit<EntityBuilderProps, 'Card'>;

export type EntityLoader = (client: MicroMClient, parentKeys?: ValuesObject) => Promise<Entity<EntityDefinition>>;

export type EntityBuilder<TEntity, TClient> = new (client: TClient, parentKeys?: ValuesObject) => TEntity;

export type EntitySource<TResult> =
    | {
        /** Builds the result synchronously */
        entityConstructor: (client: MicroMClient, parentKeys?: ValuesObject) => TResult;
        entityLoader?: never;
    }
    | {
        entityConstructor?: never;
        /** Loads the result asynchronously (e.g. via dynamic import) */
        entityLoader: (client: MicroMClient, parentKeys?: ValuesObject) => Promise<TResult>;
    };

export type EntitySourceProps = EntitySource<Entity<EntityDefinition>>;

export type EntityBuilderSourceProps = EntitySource<EntityBuilderProps>;

export type EntityGridSourceProps = EntitySource<EntityGridBuilderProps>;

export function importEntity<TClient, TEntity, TModule>
    (
        importModule: () => Promise<TModule>,
        getEntity: (module: TModule) => EntityBuilder<TEntity, TClient>
    ) {
    return async (client: TClient, parentKeys?: ValuesObject) => {
        const module = await importModule();
        const NewEntity = getEntity(module);

        return new NewEntity(client, parentKeys);
    };
}

// With importCard: produces a loader suitable for DataViewPanel (Card required)
export function getEntity<TClient, TEntity, TModule, TCard>
    (
        importModule: () => Promise<TModule>,
        getEntity: (module: TModule) => EntityBuilder<TEntity, TClient>,
        getView: (entity: TEntity) => string,
        importCard: () => Promise<TCard>
    ): (client: TClient, parentKeys?: ValuesObject) => Promise<EntityBuilderProps>;

// Without importCard: produces a loader suitable for DataGridPanel (no Card)
export function getEntity<TClient, TEntity, TModule>
    (
        importModule: () => Promise<TModule>,
        getEntity: (module: TModule) => EntityBuilder<TEntity, TClient>,
        getView: (entity: TEntity) => string
    ): (client: TClient, parentKeys?: ValuesObject) => Promise<EntityGridBuilderProps>;

export function getEntity<TClient, TEntity, TModule, TCard>
    (
        importModule: () => Promise<TModule>,
        getEntity: (module: TModule) => EntityBuilder<TEntity, TClient>,
        getView: (entity: TEntity) => string,
        importCard?: () => Promise<TCard>
    ) {
    const loadEntity = importEntity(importModule, getEntity);

    return async (client: TClient, parentKeys?: ValuesObject) => {
        const entity = await loadEntity(client, parentKeys);
        const card = importCard ? await importCard() : undefined;

        return {
            entity,
            view: getView(entity),
            Card: card
        } as EntityBuilderProps;
    };
}
