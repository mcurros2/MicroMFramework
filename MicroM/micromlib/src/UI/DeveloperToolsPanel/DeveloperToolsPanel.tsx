import { Group, Loader, useComponentDefaultProps } from "@mantine/core";
import { MicroMClient } from "../../client/MicromClient";
import { MicromEntitiesTypes, MicromEntitiesTypesDef } from "../../DataDictionary";
import { nameof } from "../../Entity";
import { DataGridPanel, DataGridPanelProps } from "../DataGrid";
import { EntityGridBuilderProps } from "../GetEntity/GetEntity";

export interface DeveloperToolsPanelProps extends Omit<DataGridPanelProps, 'parentKeys'> {
}

export const DeveloperToolsPanelDefaultProps: Partial<DeveloperToolsPanelProps> = {
    actionsButtonVariant: 'light',
    toolbarIconVariant: 'light',
    bgLight: 'gray.3',
    bgDark: undefined,
    gridHeight: 'flex-grow',
    containerCardProps: { h: '100%', withBorder: true },
    loadingComponent: <Group h="100%" align="flex-start"><Loader /></Group>,
}

export function DeveloperToolsPanel(props: DeveloperToolsPanelProps) {
    const {
        entityLoader, ...rest
    } = useComponentDefaultProps("DeveloperToolsPanel", DeveloperToolsPanelDefaultProps, props);

    return (
        <DataGridPanel
            {...rest}
            formMode="view"
            enableView={false}
            entityConstructor={(client: MicroMClient) => ({ entity: new MicromEntitiesTypes(client), view: nameof<MicromEntitiesTypesDef>(v => v.views.mty_brwStandard) } as EntityGridBuilderProps)}
        />
    );
}