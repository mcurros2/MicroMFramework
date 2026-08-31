import { Card, Text } from "@mantine/core";
import { EntityClientAction } from "../../Entity/EntityClientAction";
import { DataGrid } from "../../UI/DataGrid/DataGrid";
import { ImportProcessErrors } from "../ImportProcessErrors/ImportProcessErrors";

export const ACTViewImportErrorsLabels = {
    title: 'Import data errors',
    label: 'View import errors'
};

export const ACTViewImportErrors: EntityClientAction = {
    name: 'ACTViewImportErrors',
    title: <Text fw={700}>{ACTViewImportErrorsLabels.title}</Text>,
    label: ACTViewImportErrorsLabels.label,
    dontRequireSelection: false,
    minSelectedRecords: 1,
    maxSelectedRecords: 1,
    views: ['ipr_brwStandard'],
    onClick: async ({ entity, modal, selectedKeys, element }) => {
        if (!modal) {
            console.warn('modal context not found');
            return Promise.resolve(false);
        }

        const parentKeys = selectedKeys?.[0] ?? {};

        const import_errors = new ImportProcessErrors(entity.API.client, parentKeys);

        await modal.open({
            modalProps: {
                title: <Text fw={700}>{ACTViewImportErrorsLabels.title}</Text>,
                size: 'xl'
            },
            focusOnClosed: element,
            content: <Card>
                <DataGrid
                    entity={import_errors}
                    viewName={import_errors.def.views.ipe_brwStandard.name}
                    selectionMode="single"
                    enableAdd={false}
                    enableEdit={false}
                    enableDelete={false}
                    enableView={false}
                />
            </Card>
        });

        return false;
    }
};