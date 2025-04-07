import { useRef } from "react";
import { Entity, EntityDefinition } from "../../Entity";
import { MicroMClient } from "../../client";
import { DataView } from "./DataView";
import { DataViewProps } from "./DataView.types";


export interface DataViewPageProps extends Omit<DataViewProps, 'entity'> {
    client: MicroMClient,
    entityConstructor: (client: MicroMClient) => Entity<EntityDefinition>,
}

export function DataViewPage(props: DataViewPageProps) {
    const { client, entityConstructor } = props;

    const entity = useRef<Entity<EntityDefinition>>(entityConstructor(client));

    return (<DataView
        entity={entity.current}
        {...props}
    />)
}