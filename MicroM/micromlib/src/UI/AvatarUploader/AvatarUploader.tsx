import { ActionIcon, Avatar, AvatarProps, Group, Stack, useMantineTheme } from "@mantine/core";
import { IconTrash, IconUpload } from "@tabler/icons-react";
import { AvatarUploaderAPI } from "./useAvatarUploader";


export interface AvatarUploaderProps extends Omit<AvatarProps, 'src'> {
    API: AvatarUploaderAPI,
    PlaceHolderIcon?: React.ReactNode,
    showFullImage?: boolean,
    readOnlyMode?: boolean
}

export function AvatarUploader(props: AvatarUploaderProps) {
    const theme = useMantineTheme();
    const { API, PlaceHolderIcon, readOnlyMode, showFullImage, ...others } = props;

    const { imageURL, thumbnailURL, fileID, fileGUID, handleOpenFileUpload, handleDeleteFile, parentFormAPI } = API;

    return (
        <Stack>
            <Avatar {...others} src={ showFullImage ? imageURL ?? undefined : thumbnailURL ?? undefined} onClick={async () => {
                if (!readOnlyMode && parentFormAPI?.formMode !== 'view') await handleOpenFileUpload()
            }}>
                {PlaceHolderIcon}
            </Avatar>
            {!readOnlyMode && parentFormAPI?.formMode !== 'view' &&
                <Group position="right">
                    <ActionIcon color={theme.primaryColor} variant="light" onClick={async () => await handleOpenFileUpload()}><IconUpload size="1rem" /></ActionIcon>
                    <ActionIcon color={theme.primaryColor} variant="light" onClick={async () => await handleDeleteFile(fileID ?? '', fileGUID ?? '')}><IconTrash size="1rem" /></ActionIcon>
                </Group>
            }
        </Stack>
    );
}