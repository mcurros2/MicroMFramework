import { ActionIcon, Box, Button, Card, Group, Image, ImageProps, Progress, Stack, Text, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { Dropzone, DropzoneProps } from "@mantine/dropzone";
import { IconCircleX, IconDownload, IconEye, IconFileTypePdf, IconPhoto, IconProps, IconTrash, IconUpload, IconX } from "@tabler/icons-react";
import { ReactNode } from "react";
import { NotifyError, NotifyInfo, useModal } from "../Core";
import { ImagePreview } from "./ImagePreview";
import { PDFPreview } from "./PDFPreview";
import { getFileType } from "./getFileType";
import { UploadProgressReport, UseFileUploadReturnType } from "./useFileUpload";
import { UseEntityFormReturnType } from "../Form/useEntityForm";

export interface FileUploaderProps extends Omit<DropzoneProps, 'children' | 'onDrop' | 'maxSize' | 'maxFiles'> {
    IdleIcon?: (props: IconProps) => ReactNode,
    UploadText?: string,
    AttachUpToText?: string,
    FilesText?: string,
    EachFileShouldNotExceedText?: string,
    uploadAPI: UseFileUploadReturnType,
    onDelete?: (file_id: string) => boolean,
    imageProps?: ImageProps,
    closeText?: string,
    cancelledText?: string,
    operationCancelledText?: string,
    pdfCannotBeViewedText?: string,
    showCancelButton?: boolean,
    cancelLabel?: string,
    parentFormAPI?: UseEntityFormReturnType,
}


export const FileUploaderDefaultProps: Partial<FileUploaderProps> = {
    IdleIcon: IconPhoto,
    UploadText: 'Drag files here or click to select',
    EachFileShouldNotExceedText: 'each file should not exceed',
    AttachUpToText: 'Attach up to',
    FilesText: 'files',
    closeText: 'Close',
    cancelledText: 'Cancelled',
    operationCancelledText: 'Operation cancelled',
    pdfCannotBeViewedText: 'PDF cannot be displayed, download the file to view it.',
    accept: ['image/*'],
    imageProps: {
        width: "7rem",
        height: "3.94rem",
        fit: "contain",
        withPlaceholder: true,
        mah: "3.94rem"
    },
    showCancelButton: true,
    cancelLabel: 'Cancel'
}


export function FileUploader(props: FileUploaderProps) {
    const {
        IdleIcon, UploadText, uploadAPI, EachFileShouldNotExceedText, AttachUpToText, FilesText,
        imageProps, onDelete, closeText, cancelledText, operationCancelledText, pdfCannotBeViewedText,
        showCancelButton, cancelLabel, parentFormAPI, ...dropzoneProps
    } = useComponentDefaultProps('FileUploader', FileUploaderDefaultProps, props);

    const {
        uploadFiles, uploadProgress, errorNotification, cancelledNotification, clearNotifications, uploadingNotification, deleteFile,
        downloadFile, cancelUpload, loadingNotification
    } = uploadAPI;

    const theme = useMantineTheme();
    const modals = useModal();

    dropzoneProps.disabled = dropzoneProps.disabled || (parentFormAPI?.formMode === 'view');

    const handleUpload = async (selectedFiles: File[]) => {
        await uploadFiles(selectedFiles);
    }

    const handleDeleteFile = async (file_id: string) => {
        let result = true;
        if (onDelete) result = await onDelete(file_id);
        if (result) await deleteFile(file_id, '');

    };

    const handlePreviewImage = async (documentURL: string, fileName: string) => {
        await modals.open({
            modalProps: {
                size: "xl",
                title: <Text fw="700">{fileName}</Text>
            },
            content: <ImagePreview documentURL={documentURL} closeText={closeText!} onClose={async () => await modals.close()} />
        });
    }

    const handlePreviewPdf = async (documentURL: string, fileName: string) => {
        await modals.open({
            modalProps: {
                size: "xl",
                title: <Text fw="700">{fileName}</Text>
            },
            content: <PDFPreview documentURL={documentURL} closeText={closeText!} onClose={async () => await modals.close()} filePreviewError={pdfCannotBeViewedText} />
        });
    };

    const progressElements = Object.values(uploadProgress).map((report: UploadProgressReport) => {
        const fileType = getFileType(report.file_name);

        return (
            <Card key={report.status_id} bg={theme.colorScheme === 'dark' ? theme.colors.dark[8] : theme.colors.gray[3]} w="15rem">
                {report.done && !report.errorMessage &&
                    <Card.Section p="xs" mb="1rem">
                        <Group position="right">
                            {(fileType === 'image' || fileType === 'pdf') &&
                                <ActionIcon color={theme.primaryColor} variant="light" onClick={async () =>
                                    fileType === 'image'
                                        ? await handlePreviewImage(report.documentURL!, report.file_name)
                                        : await handlePreviewPdf(report.documentURL!, report.file_name)}>
                                    <IconEye size="1rem" />
                                </ActionIcon>
                            }
                            <ActionIcon color={theme.primaryColor} variant="light" onClick={async () => await downloadFile(report.documentURL!)}><IconDownload size="1rem" /></ActionIcon>
                            {parentFormAPI?.formMode !== 'view' &&
                                    <ActionIcon disabled={dropzoneProps.disabled} color={theme.primaryColor} variant="light" onClick={async () => await handleDeleteFile(report.file_id!)}><IconTrash size="1rem" /></ActionIcon>
                            }
                        </Group>
                    </Card.Section>
                }
                {!report.done &&
                    <>
                        <Text size="sm" color="dimmed">{report.file_name} - {report.progress}%</Text>
                        <Progress value={report.progress} striped animate />
                    </>
                }
                {report.cancelled &&
                    <Text size="sm" color="dimmed">{cancelledText}: {report.file_name}</Text>
                }
                {report.done && !report.errorMessage && fileType === 'image' &&
                    <Image {...imageProps} src={report.thumbnailURL} />
                }
                {report.done && !report.errorMessage && fileType === 'pdf' &&
                    <Group grow h="9.375rem">
                        <IconFileTypePdf size="3.2rem" stroke={1.5} />
                    </Group>
                }
                {
                    report.done && !report.errorMessage &&
                    <Box mt="xs">
                        <Text size="xs" color="dimmed" truncate>{report.file_name}</Text>
                        <Text size="xs" color="dimmed" >{(report.file_size / (1024 ** 2)).toFixed(2)}MB</Text>
                    </Box>
                }
            </Card>
        )
    });

    const ImageIcon = IdleIcon!;

    return (
        <Stack>
            <Group grow>
                <Dropzone {...dropzoneProps} loading={uploadingNotification || loadingNotification} onDrop={handleUpload}>
                    <Group position="center" spacing="xl" style={{ pointerEvents: 'none' }}>
                        <Dropzone.Accept>
                            <IconUpload
                                size="3.2rem"
                                stroke={1.5}
                                color={theme.colors[theme.primaryColor][theme.colorScheme === 'dark' ? 4 : 6]}
                            />
                        </Dropzone.Accept>
                        <Dropzone.Reject>
                            <IconX
                                size="3.2rem"
                                stroke={1.5}
                                color={theme.colors.red[theme.colorScheme === 'dark' ? 4 : 6]}
                            />
                        </Dropzone.Reject>
                        <Dropzone.Idle>
                            <ImageIcon size="3.2rem" stroke={1.5} />
                        </Dropzone.Idle>
                        <div>
                            <Text size="xl" inline>
                                {UploadText}
                            </Text>
                            <Text size="sm" color="dimmed" inline mt={7}>
                                {AttachUpToText} {uploadAPI.maxFilesCount} {FilesText}, {EachFileShouldNotExceedText} {Math.round(uploadAPI.maxIndividualFileSize! / (1024 ** 2))} MB
                            </Text>
                        </div>
                    </Group>
                </Dropzone>
            </Group>
            {showCancelButton && uploadingNotification &&
                <Group key="cancel">
                    <Button color="red" size="sm" leftIcon={<IconCircleX size="1.5rem" />} onClick={cancelUpload}>{cancelLabel}</Button>
                </Group>
            }
            {
                (errorNotification || cancelledNotification) &&
                <Group key="notifications" grow>
                    {errorNotification && !cancelledNotification &&
                        <NotifyError key="error" withCloseButton title="" onClose={clearNotifications} bg={theme.colorScheme === 'dark' ? theme.colors.dark[5] : theme.colors[theme.primaryColor][3]}>
                            {errorNotification}
                        </NotifyError>
                    }
                    {cancelledNotification &&
                        <NotifyInfo key="info" withCloseButton title="" onClose={clearNotifications} bg={theme.colorScheme === 'dark' ? theme.colors.dark[5] : theme.colors[theme.primaryColor][3]}>
                            {operationCancelledText}
                        </NotifyInfo>
                    }
                </Group>
            }
            {
                progressElements.length > 0 &&
                <Group key="progresselements" spacing="xs">
                    {progressElements}
                </Group>
            }
        </Stack >
    )
}