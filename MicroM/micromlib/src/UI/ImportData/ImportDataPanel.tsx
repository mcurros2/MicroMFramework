import { Group, Loader, useComponentDefaultProps } from "@mantine/core";
import { useCallback, useMemo } from "react";
import { MicroMClient, ValuesObject } from "../../client";
import { ImportProcess } from "../../DataDictionary/ImportProcess";
import { EntityClientActionImportDataOnClickProps } from "../../Entity";
import { DataGridPanel, DataGridPanelProps } from "../DataGrid";
import { EntityGridBuilderProps, EntityGridSourceProps, useResolvedEntityBuilder } from "../GetEntity";

type ControlledDataGridPanelProps =
    'entityConstructor'
    | 'entityLoader'
    | 'parentKeys'
    | 'enableImport'
    | 'entityProcName'
    | 'excludedImportDestinations'
    | 'clientActionOthers'
    | 'enableAdd'
    | 'enableEdit'
    | 'enableDelete'
    | 'enableView'
    | 'selectionMode'
    | 'showActions'
    | 'showActionsToolbar'
    | 'doubleClickAction'
    | 'autoSelectFirstRow'
    | 'showSelectRowsButton'
    | 'initialSelectRowsToggle';

export type ImportDataPanelDestinationSourceProps =
    | {
        /** Builds the entity that receives the imported records synchronously. */
        destinationEntityConstructor: NonNullable<EntityGridSourceProps['entityConstructor']>;
        destinationEntityLoader?: never;
    }
    | {
        destinationEntityConstructor?: never;
        /** Loads the entity that receives the imported records asynchronously. */
        destinationEntityLoader: NonNullable<EntityGridSourceProps['entityLoader']>;
    };

export type ImportDataPanelProps = Omit<DataGridPanelProps, ControlledDataGridPanelProps> & ImportDataPanelDestinationSourceProps & {
    parentKeys?: ValuesObject,
    entityProcName?: string,
    excludedImportDestinations?: string[],
    importDataLabel?: string,
    downloadImportedFileLabel?: string,
};

export const ImportDataPanelDefaultProps: Partial<ImportDataPanelProps> = {
    gridHeight: 'flex-grow',
    loadingComponent: <Group h="100%" align="flex-start"><Loader /></Group>,
    importDataLabel: "Import data",
    downloadImportedFileLabel: "Download imported file",
};

export function ImportDataPanel(props: ImportDataPanelProps) {
    const mergedProps = useComponentDefaultProps('ImportDataPanel', ImportDataPanelDefaultProps, props);

    const {
        client, destinationEntityConstructor, destinationEntityLoader, parentKeys, entityProcName, excludedImportDestinations,
        loadingComponent, importDataLabel, downloadImportedFileLabel, ...dataGridProps
    } = mergedProps;

    const { result: destinationEntityBuilder, ready: destinationEntityReady } = useResolvedEntityBuilder<EntityGridBuilderProps>(
        client,
        parentKeys,
        destinationEntityConstructor ? { entityConstructor: destinationEntityConstructor } : { entityLoader: destinationEntityLoader! }
    );

    const destinationEntity = destinationEntityBuilder?.entity;

    const historyEntityBuilder = useCallback((historyClient: MicroMClient, parentKeys?: ValuesObject) => {
        const historyEntity = new ImportProcess(historyClient, parentKeys);

        historyEntity.def.clientActions.ACTDownloadImportedFile.label = downloadImportedFileLabel;
        historyEntity.def.clientActions.ACTImportData.label = importDataLabel;

        return {
            entity: historyEntity,
            view: historyEntity.def.views.ipr_brwStandard.name,
        } as EntityGridBuilderProps;

    }, [downloadImportedFileLabel, importDataLabel]);

    const historyParentKeys = useMemo(
        () => destinationEntity ? { vc_assemblytypename: destinationEntity.name } : undefined,
        [destinationEntity]
    );

    const clientActionOthers = useMemo(() => ({
        destinationEntity: destinationEntity!,
        entityProcName,
        excludedImportDestinations,
    } satisfies EntityClientActionImportDataOnClickProps), [destinationEntity, entityProcName, excludedImportDestinations]);

    if (!destinationEntityReady || !destinationEntity) return <>{loadingComponent}</>;

    return <DataGridPanel
        {...dataGridProps}
        key={destinationEntity.name}
        client={client}
        parentKeys={historyParentKeys}
        entityConstructor={historyEntityBuilder}
        clientActionOthers={clientActionOthers}
        selectionMode="single"
        enableAdd={false}
        enableEdit={false}
        enableDelete={false}
        enableView={false}
        enableImport={false}
    />;
}
