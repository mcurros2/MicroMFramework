import { Button, Card, Group, Text, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconCircleCheck, IconInfoCircle } from "@tabler/icons-react";
import { EntityColumn } from "../../Entity";
import { FileUploader, FileUploaderProps } from "./FileUploader";
import { UploadProgressReport, useFileUpload, UseFileUploadProps, UseFileUploadReturnType, useFileUploadSnapshot } from "./useFileUpload";

export interface FilesUploadFormProps extends UseFileUploadProps {
    fileProcessColumn: EntityColumn<string>,
    onOK?: (fileprocess_id: string, files: readonly UploadProgressReport[]) => void,
    onDelete?: (fileGUID: string) => boolean | Promise<boolean>,
    helpMessage?: string,
    uploaderProps?: Omit<FileUploaderProps, 'uploadAPI' | 'editor'>,
    okLabel?: string,
    showOKButton?: boolean,
    uploadAPI?: UseFileUploadReturnType,
}

export const FilesUploadFormDefaultProps: Partial<FilesUploadFormProps> = {
    helpMessage: "Select the files that you need and click OK",
    okLabel: "Close",
    maxIndividualFileSize: 2 * (1024 ** 2),
    maxTotalFilesSize: 10 * (1024 ** 2),
    maxFilesCount: 5,
}

interface FilesUploadFormContentProps {
    uploadAPI: UseFileUploadReturnType,
    uploaderProps?: Omit<FileUploaderProps, 'uploadAPI' | 'editor'>,
    onDelete?: (fileGUID: string) => boolean | Promise<boolean>,
    onOK?: (fileprocess_id: string, files: readonly UploadProgressReport[]) => void,
    helpMessage?: string,
    okLabel?: string,
    showOKButton?: boolean,
}

function FilesUploadFormContent({
    uploadAPI, uploaderProps, onDelete, onOK, helpMessage, okLabel, showOKButton
}: FilesUploadFormContentProps) {
    const theme = useMantineTheme();
    const uploadState = useFileUploadSnapshot(uploadAPI);
    const handleOK = () => onOK?.(uploadState.fileProcessID, uploadState.files);

    return (
        <>
            <Card shadow="sm" withBorder={theme.colorScheme !== 'dark'}>
                <Card.Section p="xs" bg={theme.colorScheme === 'dark' ? theme.colors.dark[5] : theme.colors[theme.primaryColor][3]} mb="1rem">
                    <Group sx={{ gap: "0.25rem" }}>
                        <IconInfoCircle size="1.1rem" />
                        <Text fz="xs" c="dimmed">{helpMessage}</Text>
                    </Group>
                </Card.Section>
                <FileUploader {...uploaderProps} uploadAPI={uploadAPI} onDelete={onDelete} />
            </Card>
            <Group mt="md" position="right">
                {showOKButton &&
                    <Button
                        loading={uploadState.uploadingNotification}
                        disabled={uploadState.uploadingNotification}
                        onClick={handleOK}
                        color={theme.colors.green[5]}
                        leftIcon={<IconCircleCheck size="1.5rem" />}
                    >
                        {okLabel}
                    </Button>
                }
            </Group>
        </>
    );
}

function OwnedFilesUploadForm(props: Omit<FilesUploadFormProps, 'uploadAPI'>) {
    const {
        helpMessage, client, uploaderProps, okLabel, onCancel, onDelete,
        maxFilesCount, maxIndividualFileSize, maxTotalFilesSize, onOK, fileProcessColumn,
        youCanUploadAMaximumOfText, filesText, exceedMaximumIndividualSizeText,
        unspecifiedErrorWhenUploadingFileText, totalUploadExceedsMaximumSizeText,
        showOKButton, editor, imageProcessing, imageEditorTitle, imageEditorProps,
        imageEditorModalProps, onValidateFile, onProcessFile, onBeforeUpload,
        onBeforeReplace, onUploadComplete, thumbnailMaxSize, thumbnailQuality,
        loadFilesOnMount
    } = props;

    const uploadAPI = useFileUpload({
        client,
        fileProcessColumn,
        maxFilesCount,
        maxIndividualFileSize,
        maxTotalFilesSize,
        youCanUploadAMaximumOfText,
        filesText,
        exceedMaximumIndividualSizeText,
        unspecifiedErrorWhenUploadingFileText,
        totalUploadExceedsMaximumSizeText,
        onCancel,
        editor,
        imageProcessing,
        imageEditorTitle,
        imageEditorProps,
        imageEditorModalProps,
        onValidateFile,
        onProcessFile,
        onBeforeUpload,
        onBeforeReplace,
        onUploadComplete,
        thumbnailMaxSize,
        thumbnailQuality,
        loadFilesOnMount
    });

    return <FilesUploadFormContent
        uploadAPI={uploadAPI}
        uploaderProps={uploaderProps}
        onDelete={onDelete}
        onOK={onOK}
        helpMessage={helpMessage}
        okLabel={okLabel}
        showOKButton={showOKButton}
    />;
}

export function FilesUploadForm(props: FilesUploadFormProps) {
    const resolvedProps = useComponentDefaultProps('FilesUploadForm', FilesUploadFormDefaultProps, props);
    const { uploadAPI, ...ownedProps } = resolvedProps;

    if (uploadAPI) {
        return <FilesUploadFormContent
            uploadAPI={uploadAPI}
            uploaderProps={resolvedProps.uploaderProps}
            onDelete={resolvedProps.onDelete}
            onOK={resolvedProps.onOK}
            helpMessage={resolvedProps.helpMessage}
            okLabel={resolvedProps.okLabel}
            showOKButton={resolvedProps.showOKButton}
        />;
    }

    return <OwnedFilesUploadForm {...ownedProps} />;
}
