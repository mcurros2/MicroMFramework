import { Group, Text } from "@mantine/core";
import { IconDownload } from "@tabler/icons-react";
import { EntityClientAction } from "../../Entity";

export const ACTDownloadImportedFileLabels = {
    label: 'Download imported file',
    error: 'The imported file could not be downloaded.',
};

export const ACTDownloadImportedFile: EntityClientAction = {
    name: 'ACTDownloadImportedFile',
    title: <Group spacing="xs"><IconDownload size="1rem"/><Text fw={700}>{ACTDownloadImportedFileLabels.label}</Text></Group>,
    label: ACTDownloadImportedFileLabels.label,
    icon: <IconDownload size="1rem" />,
    minSelectedRecords: 1,
    maxSelectedRecords: 1,
    showActionInViewMode: true,
    views: ['ipr_brwStandard'],
    onClick: async ({ entity, selectedKeys }) => {
        const fileGUID = selectedKeys?.[0]?.vc_fileguid;

        if (typeof fileGUID !== 'string' || !fileGUID) {
            console.warn(ACTDownloadImportedFileLabels.error);
            return false;
        }

        try {
            const client = entity.API.client;
            const blob = await client.downloadBlob(client.getDocumentURL(fileGUID));
            entity.API.downloadBlobFile(blob, fileGUID);
            return true;
        }
        catch (error: unknown) {
            console.error(ACTDownloadImportedFileLabels.error, error);
            return false;
        }
    },
};
