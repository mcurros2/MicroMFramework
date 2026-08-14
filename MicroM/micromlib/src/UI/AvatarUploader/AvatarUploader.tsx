import { ActionIcon, Alert, Avatar, AvatarProps, Group, Loader, Stack, useMantineTheme } from "@mantine/core";
import { IconEdit, IconTrash, IconUpload } from "@tabler/icons-react";
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

    const {
        imageURL, thumbnailURL, fileGUID, handleOpenFileUpload, handleEditImage, handleDeleteFile,
        parentFormAPI, canEditImage, processing, errorNotification, clearNotifications, labels
    } = API;

    const editable = !readOnlyMode && parentFormAPI?.formMode !== 'view';

    const handleAvatarClick = async () => {
        if (!editable || processing) return;
        if (canEditImage) await handleEditImage();
        else await handleOpenFileUpload();
    };

    return (
        <Stack>
            <Avatar
                {...others}
                src={showFullImage ? imageURL ?? undefined : thumbnailURL ?? imageURL ?? undefined}
                onClick={() => void handleAvatarClick()}
                sx={editable ? { cursor: processing ? 'wait' : 'pointer' } : undefined}
                aria-busy={processing}
            >
                {processing ? <Loader size="sm" /> : PlaceHolderIcon}
            </Avatar>
            {editable &&
                <Group position="right">
                    {canEditImage &&
                        <ActionIcon
                            color={theme.primaryColor}
                            variant="light"
                            title={labels.editLabel}
                            aria-label={labels.editLabel}
                            disabled={processing}
                            onClick={() => void handleEditImage()}
                        >
                            <IconEdit size="1rem" />
                        </ActionIcon>
                    }
                    <ActionIcon
                        color={theme.primaryColor}
                        variant="light"
                        title={labels.uploadLabel}
                        aria-label={labels.uploadLabel}
                        disabled={processing}
                        onClick={() => void handleOpenFileUpload()}
                    >
                        <IconUpload size="1rem" />
                    </ActionIcon>
                    <ActionIcon
                        color={theme.primaryColor}
                        variant="light"
                        title={labels.deleteLabel}
                        aria-label={labels.deleteLabel}
                        disabled={processing || !fileGUID}
                        onClick={() => void handleDeleteFile(fileGUID ?? '')}
                    >
                        <IconTrash size="1rem" />
                    </ActionIcon>
                </Group>
            }
            {errorNotification &&
                <Alert color="red" withCloseButton onClose={clearNotifications}>
                    {errorNotification}
                </Alert>
            }
        </Stack>
    );
}
