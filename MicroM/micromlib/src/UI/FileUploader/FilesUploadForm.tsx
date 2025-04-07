import { Button, Card, Group, Text, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconCircleCheck, IconInfoCircle } from "@tabler/icons-react";
import { EntityColumn } from "../../Entity";
import { FileUploader, FileUploaderProps } from "./FileUploader";
import { UploadProgressReport, UseFileUploadProps, ValidateFileReturnType, useFileUpload } from "./useFileUpload";

export interface FilesUploadFormProps extends UseFileUploadProps {
    fileProcessColumn: EntityColumn<string>,
    onOK?: (fileprocess_id: string, uploadProgress: Record<string, UploadProgressReport>) => void,
    onDelete?: (file_id: string) => boolean,
    helpMessage?: string,
    uploaderProps?: Omit<FileUploaderProps, 'uploadAPI' | 'abortSignal'>,
    okLabel?: string,
    showOKButton?: boolean,
    editor?: 'none' | 'image',
    onValidateFile?: (file: File) => Promise<ValidateFileReturnType>,
}

export const FilesUploadFormDefaultProps: Partial<FilesUploadFormProps> = {
    helpMessage: "Select the files that you need and click OK",
    okLabel: "Close",
    maxIndividualFileSize: 2 * (1024 ** 2),
    maxTotalFilesSize: 10 * (1024 ** 2),
    maxFilesCount: 5,
}

export function FilesUploadForm(props: FilesUploadFormProps) {
    const {
        helpMessage, client, uploaderProps, okLabel, onCancel,
        maxFilesCount, maxIndividualFileSize, maxTotalFilesSize, onOK, fileProcessColumn,
        showOKButton, editor, onValidateFile
    } = useComponentDefaultProps('FilesUploadForm', FilesUploadFormDefaultProps, props);


    const theme = useMantineTheme();
    const uploadAPI = useFileUpload({ client, fileProcessColumn, maxFilesCount, maxIndividualFileSize, maxTotalFilesSize, onCancel, editor, onValidateFile });
    const { uploadingNotification } = uploadAPI;

    const handleOK = () => {
        
        if (onOK) onOK(uploadAPI.fileProcessID, uploadAPI.uploadProgress);
    };

    return (
        <>
            <Card shadow="sm" withBorder={theme.colorScheme === 'dark' ? false : true}>
                <Card.Section p="xs" bg={theme.colorScheme === 'dark' ? theme.colors.dark[5] : theme.colors[theme.primaryColor][3]} mb="1rem">
                    <Group sx={{ gap: "0.25rem" }}>
                        <IconInfoCircle size="1.1rem" />
                        <Text fz="xs" c="dimmed">{helpMessage}</Text>
                    </Group>
                </Card.Section>
                <FileUploader {...uploaderProps} uploadAPI={uploadAPI} />
            </Card>
            <Group mt="md" position="right">
                {showOKButton &&
                    <Button loading={uploadingNotification} disabled={uploadingNotification} onClick={handleOK} color={theme.colors.green[5]} leftIcon={<IconCircleCheck size="1.5rem" />}>{okLabel}</Button>
                }
            </Group>
        </>
    )
}