import { useRef } from "react";
import { MicroMClient } from "../../client/MicromClient";
import { MicromEntitiesTypes, MicromEntitiesTypesDef } from "../../DataDictionary";
import { Entity, EntityDefinition, nameof } from "../../Entity";
import { DataGrid, DataGridProps } from "../DataGrid";

export interface DeveloperToolsPanelProps extends Omit<DataGridProps, 'entity' | 'title' | 'formMode'> {
    client: MicroMClient,
}

export function DeveloperToolsPanel({ client, ...rest }: DeveloperToolsPanelProps) {
    const entity = useRef<Entity<EntityDefinition>>(new MicromEntitiesTypes(client));

    return (
        <DataGrid
            {...rest}
            formMode="view"
            viewName={nameof<MicromEntitiesTypesDef>(v => v.views.mty_brwStandard)}
            entity={entity.current}
            gridHeight="flex-grow"
        />
    );
}