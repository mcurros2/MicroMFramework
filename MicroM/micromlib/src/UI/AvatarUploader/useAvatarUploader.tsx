import { Text, useComponentDefaultProps } from "@mantine/core";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { MicroMClient } from "../../client";
import { EntityColumn } from "../../Entity";
import { ConfirmAndExecutePanel, useModal } from "../Core";
import { UploadProgressReport, useFilesUploadForm, useFileUpload } from "../FileUploader";
import { ImageEditor } from "../FileUploader/ImageEditor";
import { BrowserImageFormat, BrowserImageProcessingOptions, getImageOutputSettings, hasInteractiveImageProcessing, processImageFileAutomatically, resolveImageProcessingOptions } from "../FileUploader/imageProcessing";
import { UseEntityFormReturnType } from "../Form";

export type AvatarImageFormat = BrowserImageFormat;
export type AvatarImageProcessingOptions = BrowserImageProcessingOptions;

export interface useAvatarUploaderProps {
    client: MicroMClient,
    fileProcessColumn: EntityColumn<string>,
    fileGUIDColumn: EntityColumn<string>,
    initialImageURL?: string,
    labels?: AvatarUploaderLabels,
    parentFormAPI?: UseEntityFormReturnType,
    maxFileSize?: number,
    imageProcessing?: AvatarImageProcessingOptions,
}

export type AvatarUploaderLabels = {
    modalTitle?: string,
    editorTitle?: string,
    editLabel?: string,
    uploadLabel?: string,
    deleteLabel?: string,
    saveLabel?: string,
    cancelLabel?: string,
    rotateClockwiseLabel?: string,
    rotateCounterClockwiseLabel?: string,
    replaceTitle?: string,
    replaceMessage?: string,
    replaceConfirmLabel?: string,
    replaceCancelLabel?: string,
    replacementFailedMessage?: string,
}

const AvatarUploaderDefaultLabels: Required<AvatarUploaderLabels> = {
    modalTitle: 'Upload Image',
    editorTitle: 'Edit Image',
    editLabel: 'Edit image',
    uploadLabel: 'Upload image',
    deleteLabel: 'Delete image',
    saveLabel: 'Save',
    cancelLabel: 'Cancel',
    rotateClockwiseLabel: 'Rotate clockwise',
    rotateCounterClockwiseLabel: 'Rotate counter-clockwise',
    replaceTitle: 'Replace image?',
    replaceMessage: 'The existing uploaded image will be deleted after the replacement is uploaded successfully.',
    replaceConfirmLabel: 'Replace image',
    replaceCancelLabel: 'Keep current image',
    replacementFailedMessage: 'The replacement could not be completed. The current image was kept.',
};

export const AvatarUploaderDefaultProps: Partial<useAvatarUploaderProps> = {
    labels: AvatarUploaderDefaultLabels,
    imageProcessing: { exifOrientation: true }
};

export interface AvatarUploaderAPI {
    imageURL?: string,
    thumbnailURL?: string,
    fileID?: string,
    fileProcessID?: string,
    fileGUID?: string,
    handleOpenFileUpload: () => Promise<void>,
    handleEditImage: () => Promise<void>,
    handleDeleteFile: (file_id: string, fileGUID: string) => Promise<void>,
    parentFormAPI?: UseEntityFormReturnType,
    canEditImage: boolean,
    processing: boolean,
    errorNotification?: string,
    clearNotifications: () => void,
    labels: Required<AvatarUploaderLabels>,
}

export function useAvatarUploader(props: useAvatarUploaderProps): AvatarUploaderAPI {
    const {
        client, fileProcessColumn, labels: suppliedLabels, initialImageURL, parentFormAPI, fileGUIDColumn,
        maxFileSize, imageProcessing
    } = useComponentDefaultProps('AvatarUploader', AvatarUploaderDefaultProps, props);

    const labels = useMemo(
        () => ({ ...AvatarUploaderDefaultLabels, ...suppliedLabels }),
        [suppliedLabels]
    );
    const processingOptions = useMemo(
        () => resolveImageProcessingOptions(imageProcessing),
        [imageProcessing]
    );
    const interactiveProcessing = hasInteractiveImageProcessing(processingOptions);
    const imageFileUploadOpen = useFilesUploadForm();
    const modals = useModal();

    const fileSizeProps = useMemo(() => maxFileSize === undefined
        ? {}
        : { maxIndividualFileSize: maxFileSize, maxTotalFilesSize: maxFileSize }, [maxFileSize]);

    const deletionAPI = useFileUpload({
        client,
        maxFilesCount: 1,
        ...fileSizeProps,
        fileProcessColumn,
        loadFilesOnMount: false
    });

    const [imageURL, setImageURL] = useState<string | undefined>(initialImageURL);
    const [thumbnailURL, setThumbnailURL] = useState<string>();
    const [fileID, setFileID] = useState<string>();
    const [fileProcessID, setFileProcessID] = useState<string>();
    const [fileGUID, setFileGUID] = useState<string>();
    const [editing, setEditing] = useState(false);
    const currentFile = useRef<{ fileID?: string, fileGUID?: string }>({});
    currentFile.current = { fileID, fileGUID };

    const processFile = useCallback(async (file: File): Promise<File | null> => {
        if (!interactiveProcessing) {
            return await processImageFileAutomatically(file, processingOptions);
        }

        getImageOutputSettings(file, processingOptions);

        const source = URL.createObjectURL(file);
        return await new Promise<File | null>(async resolveEditor => {
            let closeHandled = false;

            const close = async (result: File | null) => {
                if (closeHandled) return;
                closeHandled = true;
                await modals.close();
                resolveEditor(result);
            };

            await modals.open({
                content: <ImageEditor
                    src={source}
                    sourceFile={file}
                    options={processingOptions}
                    saveLabel={labels.saveLabel}
                    cancelLabel={labels.cancelLabel}
                    rotateClockwiseLabel={labels.rotateClockwiseLabel}
                    rotateCounterClockwiseLabel={labels.rotateCounterClockwiseLabel}
                    onSave={async result => await close(result)}
                    onCancel={async () => await close(null)}
                />,
                modalProps: {
                    title: <Text fw="700">{labels.editorTitle}</Text>,
                    size: 'lg',
                    closeOnClickOutside: false,
                    closeOnEscape: false,
                    withCloseButton: false
                },
                onClosed: () => {
                    URL.revokeObjectURL(source);
                    if (!closeHandled) {
                        closeHandled = true;
                        resolveEditor(null);
                    }
                }
            });
        });
    }, [interactiveProcessing, labels, modals, processingOptions]);

    const confirmReplacement = useCallback(async () => {
        if (!currentFile.current.fileGUID) return true;

        return await new Promise<boolean>(async resolveConfirmation => {
            let closeHandled = false;

            const close = async (result: boolean) => {
                if (closeHandled) return;
                closeHandled = true;
                await modals.close();
                resolveConfirmation(result);
            };

            await modals.open({
                content: <ConfirmAndExecutePanel
                    content={<Text>{labels.replaceMessage}</Text>}
                    operation="other"
                    okButtonText={labels.replaceConfirmLabel}
                    cancelButtonText={labels.replaceCancelLabel}
                    cancelButtonProps={{ color: 'gray' }}
                    onOK={async () => await close(true)}
                    onCancel={async () => await close(false)}
                />,
                modalProps: {
                    title: <Text fw="700">{labels.replaceTitle}</Text>,
                    size: 'sm',
                    closeOnClickOutside: false,
                    closeOnEscape: false,
                    withCloseButton: false
                },
                onClosed: () => {
                    if (!closeHandled) {
                        closeHandled = true;
                        resolveConfirmation(false);
                    }
                }
            });
        });
    }, [labels, modals]);

    const commitUploadedFile = useCallback(async (report: UploadProgressReport, removeFromQueue: boolean) => {
        const previousFile = currentFile.current;
        if (previousFile.fileGUID) {
            const deleteResult = await deletionAPI.deleteFile(previousFile.fileID ?? '', previousFile.fileGUID);
            if (deleteResult.Failed) {
                return {
                    error: true,
                    message: labels.replacementFailedMessage,
                    rollbackUploadedFile: true,
                    removeFromQueue
                };
            }
        }

        fileGUIDColumn.value = report.vc_fileguid!;
        setFileGUID(report.vc_fileguid);
        setImageURL(report.documentURL ?? client.getDocumentURL(report.vc_fileguid!));
        setThumbnailURL(report.thumbnailURL ?? client.getThumbnailURL(report.vc_fileguid!));
        setFileID(report.file_id);
        setFileProcessID(fileProcessColumn.value);

        return { error: false, removeFromQueue };
    }, [client, deletionAPI, fileGUIDColumn, fileProcessColumn, labels.replacementFailedMessage]);

    const directUploadAPI = useFileUpload({
        client,
        maxFilesCount: 1,
        ...fileSizeProps,
        fileProcessColumn,
        loadFilesOnMount: false,
        onProcessFile: processFile,
        onBeforeUpload: confirmReplacement,
        onUploadComplete: async report => await commitUploadedFile(report, true)
    });

    const handleOpenFileUpload = useCallback(async () => {
        await imageFileUploadOpen({
            fileProcessColumn,
            client,
            modalTitle: labels.modalTitle,
            onOK: fileprocess_id => {
                if (fileprocess_id) setFileProcessID(fileprocess_id);
            },
            modalProps: {
                closeOnClickOutside: false,
                closeOnEscape: false,
                withCloseButton: false,
                size: "lg"
            },
            uploaderProps: {
                accept: ["image/*"],
            },
            filesUploadFormProps: {
                maxFilesCount: 1,
                ...fileSizeProps,
                loadFilesOnMount: false,
                onProcessFile: processFile,
                onBeforeUpload: confirmReplacement,
                onUploadComplete: async report => await commitUploadedFile(report, false)
            }
        });
    }, [client, commitUploadedFile, confirmReplacement, fileProcessColumn, fileSizeProps, imageFileUploadOpen, labels.modalTitle, processFile]);

    const handleEditImage = useCallback(async () => {
        if (!interactiveProcessing || !imageURL || editing) return;

        setEditing(true);
        try {
            const blob = await client.downloadBlob(imageURL);
            const extension = fileGUID?.split('.').pop();
            const name = `avatar${extension ? `.${extension}` : ''}`;
            const inferredType = extension?.toLowerCase() === 'png'
                ? 'image/png'
                : extension?.toLowerCase() === 'webp' ? 'image/webp' : 'image/jpeg';
            const file = new File([blob], name, { type: blob.type || inferredType, lastModified: Date.now() });
            await directUploadAPI.uploadFiles([file]);
        }
        finally {
            setEditing(false);
        }
    }, [client, directUploadAPI, editing, fileGUID, imageURL, interactiveProcessing]);

    const handleDeleteFile = useCallback(async (file_id: string, guid: string) => {
        if (!file_id && !guid) return;
        const result = await deletionAPI.deleteFile(file_id, guid);
        if (result.Failed) return;

        fileGUIDColumn.value = '';
        fileProcessColumn.value = '';
        setFileProcessID(undefined);
        setFileID(undefined);
        setImageURL(undefined);
        setThumbnailURL(undefined);
        setFileGUID(undefined);
    }, [deletionAPI, fileGUIDColumn, fileProcessColumn]);

    useEffect(() => {
        if (parentFormAPI?.status.loading === false && parentFormAPI.status.operationType === 'get') {
            if (fileProcessColumn.value && fileGUIDColumn.value) {
                setFileProcessID(fileProcessColumn.value);
                setImageURL(client.getDocumentURL(fileGUIDColumn.value));
                setThumbnailURL(client.getThumbnailURL(fileGUIDColumn.value));
                setFileGUID(fileGUIDColumn.value);
            }
        }
    }, [client, fileGUIDColumn, fileProcessColumn, parentFormAPI]);

    const clearNotifications = useCallback(() => {
        directUploadAPI.clearNotifications?.();
        deletionAPI.clearNotifications?.();
    }, [deletionAPI, directUploadAPI]);

    return {
        imageURL,
        thumbnailURL,
        fileID,
        fileProcessID,
        fileGUID,
        handleOpenFileUpload,
        handleEditImage,
        handleDeleteFile,
        parentFormAPI,
        canEditImage: interactiveProcessing && !!imageURL && !!fileGUID,
        processing: editing || !!directUploadAPI.uploadingNotification || !!directUploadAPI.loadingNotification || !!deletionAPI.loadingNotification,
        errorNotification: directUploadAPI.errorNotification || deletionAPI.errorNotification,
        clearNotifications,
        labels
    };
}
