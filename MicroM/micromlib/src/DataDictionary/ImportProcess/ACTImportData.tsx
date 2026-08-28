import { Group, Text } from "@mantine/core";
import { IconCloudUpload } from "@tabler/icons-react";
import { Entity, EntityClientAction } from "../../Entity";
import { openEntityForm } from "../../UI/Core/openEntityForm";
import { ImportEntityData } from "../ImportEntityData/ImportEntityData";
import { ImportEntityDataFormProps } from "../ImportEntityData/ImportEntityDataForm";
import type { ImportProcessDef } from "./ImportProcessDef";

export const ACTImportDataLabels = {
    label: 'Import data',
};

export const ACTImportData: EntityClientAction = {
    name: 'ACTImportData',
    title: <Group spacing="xs"><IconCloudUpload size="1rem" /><Text fw={700}>{ACTImportDataLabels.label}</Text></Group>,
    label: ACTImportDataLabels.label,
    icon: <IconCloudUpload size="1rem" />,
    dontRequireSelection: true,
    refreshOnClose: true,
    showActionInViewMode: false,
    views: ['ipr_brwStandard'],
    onClick: async ({ entity, modal, element, onClose }) => {
        const { destinationEntity, entityProcName, excludedImportDestinations } =
            (entity.def as ImportProcessDef).destinationProps;

        if (entityProcName && !destinationEntity.def.procs[entityProcName]) {
            console.warn(`Import data action: procedure '${entityProcName}' was not found in entity '${destinationEntity.name}'.`);
            return false;
        }

        if (!modal) {
            console.warn('Import data action: modal context was not found.');
            return false;
        }

        const importEntity = Entity.clone(destinationEntity);
        importEntity.parentKeys = { ...destinationEntity.parentKeys };
        const importData = new ImportEntityData(importEntity.API.client);

        await openEntityForm<ImportEntityDataFormProps>({
            modals: modal,
            element,
            modalFormSize: 'xl',
            handleModalCancel: async () => await modal.close(),
            handleModalSaved: async () => await modal.close(),
            formProps: {
                entity: importData,
                initialFormMode: 'add',
                importEntity,
                entityProcName,
                excludedImportDestinations,
                onImportSuccess: async () => {
                    await onClose?.(true);
                },
            },
        });

        return true;
    },
};
