import { Group, Loader, useComponentDefaultProps } from "@mantine/core";
import { useCallback, useMemo } from "react";
import { MicroMClient, ValuesObject } from "../../client";
import { ImportProcess } from "../../DataDictionary/ImportProcess";
import { DataGridPanel, DataGridPanelProps } from "../DataGrid";
import { EntityGridBuilderProps, useResolvedEntityBuilder } from "../GetEntity";

export type ImportDataPanelProps = DataGridPanelProps & {
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
        client, entityConstructor: _entityConstructor, entityLoader: _entityLoader, parentKeys, entityProcName, excludedImportDestinations,
        loadingComponent, importDataLabel, downloadImportedFileLabel, ...dataGridProps
    } = mergedProps;

    const { result: destinationEntityBuilder, ready: destinationEntityReady } = useResolvedEntityBuilder<EntityGridBuilderProps>(
        client,
        parentKeys,
        props // need entityLoader and entityConstructor to be passed in props, so that useResolvedEntityBuilder can resolve the destination entity
    );

    const destinationEntity = destinationEntityBuilder?.entity;

    const historyEntityBuilder = useCallback((historyClient: MicroMClient, parentKeys?: ValuesObject): EntityGridBuilderProps => {
        if (!destinationEntity) throw new Error('The import destination is not available.');

        const historyEntity = new ImportProcess(historyClient, parentKeys, {
            destinationEntity,
            entityProcName,
            excludedImportDestinations,
        });

        historyEntity.def.clientActions.ACTDownloadImportedFile.label = downloadImportedFileLabel;
        historyEntity.def.clientActions.ACTImportData.label = importDataLabel;

        return {
            entity: historyEntity,
            view: historyEntity.def.views.ipr_brwStandard.name,
        };

    }, [destinationEntity, downloadImportedFileLabel, entityProcName, excludedImportDestinations, importDataLabel]);

    const historyParentKeys = useMemo(
        () => destinationEntity ? { vc_assemblytypename: destinationEntity.name } : undefined,
        [destinationEntity]
    );

    return (
        <>
            {(!destinationEntityReady || !destinationEntity)
                ? <>{loadingComponent}</>
                : <DataGridPanel
                    {...dataGridProps}
                    client={client}
                    parentKeys={historyParentKeys}
                    entityConstructor={historyEntityBuilder}
                    selectionMode="single"
                    enableAdd={false}
                    enableEdit={false}
                    enableDelete={false}
                    enableView={false}
                    enableImport={false}
                />

            }
        </>
    )
}
