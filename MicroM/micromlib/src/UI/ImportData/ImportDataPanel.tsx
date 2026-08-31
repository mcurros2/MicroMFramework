import { Group, Loader, useComponentDefaultProps } from "@mantine/core";
import { useCallback, useMemo } from "react";
import { MicroMClient, ValuesObject } from "../../client";
import { ImportFileStepCustomizationProps } from "../../DataDictionary/ImportEntityData";
import { ImportProcess } from "../../DataDictionary/ImportProcess";
import { DataGridPanel, DataGridPanelProps } from "../DataGrid";
import { EntityGridBuilderProps, useResolvedEntityBuilder } from "../GetEntity";

export type ImportDataPanelProps = DataGridPanelProps & {
    destinationEntityExportViewName?: string,
    importFileStepProps?: ImportFileStepCustomizationProps,
};

export const ImportDataPanelDefaultProps: Partial<ImportDataPanelProps> = {
    gridHeight: 'flex-grow',
    loadingComponent: <Group h="100%" align="flex-start"><Loader /></Group>,
};

export function ImportDataPanel(props: ImportDataPanelProps) {
    const mergedProps = useComponentDefaultProps('ImportDataPanel', ImportDataPanelDefaultProps, props);

    const {
        client, entityConstructor: _entityConstructor, entityLoader: _entityLoader, parentKeys, entityProcName, excludedImportDestinations,
        destinationEntityExportViewName, importFileStepProps, loadingComponent, ...dataGridProps
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
            destinationEntityExportViewName,
            entityProcName,
            excludedImportDestinations,
            importFileStepProps,
        });

        return {
            entity: historyEntity,
            view: historyEntity.def.views.ipr_brwStandard.name,
        };

    }, [destinationEntity, destinationEntityExportViewName, entityProcName, excludedImportDestinations, importFileStepProps]);

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
