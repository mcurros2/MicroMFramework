import { Skeleton, useComponentDefaultProps } from "@mantine/core";
import { ReactNode, useEffect, useRef, useState } from "react";
import { Entity, EntityDefinition } from "../../Entity";
import { MicroMClient } from "../../client";
import { FormOptions, createEntityForm } from "../Core";

export interface EntityFormPageProps extends Omit<FormOptions<Entity<any>>, 'entity'> {
    entityConstructor: (client: MicroMClient) => Entity<EntityDefinition>,
    client: MicroMClient,
    defaultLoadingComponent?: ReactNode,
}

export const EntityFormPagePropsDefaultProps: Partial<EntityFormPageProps> = {
    saveAndGetOnSubmit: true,
    defaultLoadingComponent: <Skeleton />,
}

export function EntityFormPage(props: EntityFormPageProps) {
    const {
        entityConstructor, client, defaultLoadingComponent,
        ...rest
    } = useComponentDefaultProps('EntityFormPage', EntityFormPagePropsDefaultProps, props);

    const [entityForm, setEntityForm] = useState<React.ReactNode | null>(null);

    const entityRef = useRef<Entity<EntityDefinition> | null>(null);

    // MMC: note that rest properties are not used in the useEffect dependency array as using ...rest will create a new object on every render
    useEffect(() => {
        if (!entityRef.current) entityRef.current = entityConstructor(client);
        if (!entityRef.current.Form) {
            setEntityForm(<></>);
            return;
        }

        const maybePromiseForm = createEntityForm({ entity: entityRef.current!, ...rest });

        Promise.resolve(maybePromiseForm).then(resolvedForm => {
            setEntityForm(resolvedForm);
        });
    }, [client, entityConstructor]);

    return <>{entityForm || defaultLoadingComponent}</>
}