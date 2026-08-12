import { Text, useComponentDefaultProps } from "@mantine/core";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { MicroMClient } from "../../client";
import { EntityColumn } from "../../Entity";
import { ConfirmAndExecutePanel, useModal } from "../Core";
import { UploadProgressReport, useFilesUploadForm, useFileUpload } from "../FileUploader";
import { ImageEditor } from "../FileUploader/ImageEditor";
import { BrowserImageProcessingOptions, getImageOutputSettings, processImageFileAutomatically, resolveImageProcessingOptions } from "../FileUploader/imageProcessing";
import { UseEntityFormReturnType } from "../Form";

export type AvatarImageProcessingOptions = BrowserImageProcessingOptions;

export interface useAvatarUploaderProps {
    client: MicroMClient,
    fileProcessColumn: EntityColumn<string>,
    fileGUIDColumn: EntityColumn<string>,
    initialImageURL?: string,
    labels?: AvatarUploaderLabels,
    parentFormAPI?: UseEntityFormReturnType,
    maxFileSize?: number,
    editor?: boolean,
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
};

export const AvatarUploaderDefaultProps: Partial<useAvatarUploaderProps> = {
    labels: AvatarUploaderDefaultLabels,
    editor: false,
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
        maxFileSize, editor, imageProcessing
    } = useComponentDefaultProps('AvatarUploader', AvatarUploaderDefaultProps, props);

    const labels = useMemo(
        () => ({ ...AvatarUploaderDefaultLabels, ...suppliedLabels }),
        [suppliedLabels]
    );

    const processingOptions = useMemo(() => resolveImageProcessingOptions(editor === true ? {
        ...imageProcessing,
        crop: imageProcessing?.crop ?? true,
        manualRotation: imageProcessing?.manualRotation ?? true
    } : imageProcessing), [editor, imageProcessing]);
    const editorEnabled = editor === true;
    const imageFileUploadOpen = useFilesUploadForm();
    const modals = useModal();

    const fileSizeProps = useMemo(() => maxFileSize === undefined
        ? {}
        : { maxIndividualFileSize: maxFileSize, maxTotalFilesSize: maxFileSize }, [maxFileSize]);

    const deletionAPI = useFileUpload({
        client,
        maxFilesCount: 1,
        ...fileSizeProps,
        fileProcessColumn
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
        if (!editorEnabled) {
            return await processImageFileAutomatically(file, processingOptions);
        }

        getImageOutputSettings(file, processingOptions);

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
                    if (!closeHandled) {
                        closeHandled = true;
                        resolveEditor(null);
                    }
                }
            });
        });
    }, [editorEnabled, labels, modals, processingOptions]);

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
        fileGUIDColumn.value = report.vc_fileguid!;
        setFileGUID(report.vc_fileguid);
        setImageURL(report.documentURL ?? client.getDocumentURL(report.vc_fileguid!));
        setThumbnailURL(report.thumbnailURL ?? client.getThumbnailURL(report.vc_fileguid!));
        setFileID(report.file_id);
        setFileProcessID(fileProcessColumn.value);

        return { error: false, removeFromQueue };
    }, [client, fileGUIDColumn, fileProcessColumn]);

    const directUploadAPI = useFileUpload({
        client,
        maxFilesCount: 1,
        ...fileSizeProps,
        fileProcessColumn,
        loadFilesOnMount: false,
        editor: editorEnabled,
        onProcessFile: processFile,
        onBeforeUpload: confirmReplacement,
        onUploadComplete: async report => await commitUploadedFile(report, true)
    });

    const handleOpenFileUpload = useCallback(async () => {

        await imageFileUploadOpen({
            fileProcessColumn,
            client,
            modalTitle: labels.modalTitle,
            onOK: (fileprocess_id, uploadProgress) => {
                const report = Object.values(uploadProgress).find(item => item.done && !item.errorMessage && item.vc_fileguid);
                if (report?.vc_fileguid) {
                    fileProcessColumn.value = fileprocess_id;
                    fileGUIDColumn.value = report.vc_fileguid;
                    setFileProcessID(fileprocess_id);
                    setFileGUID(report.vc_fileguid);
                    setFileID(report.file_id);
                    setImageURL(report.documentURL ?? client.getDocumentURL(report.vc_fileguid));
                    setThumbnailURL(report.thumbnailURL ?? client.getThumbnailURL(report.vc_fileguid));
                }
                else {
                    fileProcessColumn.value = '';
                    fileGUIDColumn.value = '';
                    setFileProcessID(undefined);
                    setFileGUID(undefined);
                    setFileID(undefined);
                    setImageURL(undefined);
                    setThumbnailURL(undefined);
                }
            },
            modalProps: {
                closeOnClickOutside: false,
                closeOnEscape: false,
                withCloseButton: false,
                size: "lg"
            },
            uploaderProps: {
                accept: ["image/*"],
                editImageLabel: labels.editLabel
            },
            filesUploadFormProps: {
                maxFilesCount: 1,
                ...fileSizeProps,
                editor: editorEnabled,
                onProcessFile: processFile,
                onBeforeUpload: confirmReplacement
            }
        });

    }, [client, confirmReplacement, editorEnabled, fileGUIDColumn, fileProcessColumn, fileSizeProps, imageFileUploadOpen, labels.editLabel, labels.modalTitle, processFile]);

    const handleEditImage = useCallback(async () => {
        if (!editorEnabled || !imageURL || !fileGUID || editing) return;

        setEditing(true);
        try {
            await directUploadAPI.editImage({
                status_id: fileID ?? fileGUID,
                file_id: fileID,
                file_name: fileGUID,
                file_size: 0,
                progress: 100,
                done: true,
                documentURL: imageURL,
                thumbnailURL,
                vc_fileguid: fileGUID
            });
        }
        finally {
            setEditing(false);
        }
    }, [directUploadAPI, editing, editorEnabled, fileGUID, fileID, imageURL, thumbnailURL]);

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
        canEditImage: editorEnabled && !!imageURL && !!fileGUID,
        processing: editing || !!directUploadAPI.uploadingNotification || !!directUploadAPI.loadingNotification || !!deletionAPI.loadingNotification,
        errorNotification: directUploadAPI.errorNotification || deletionAPI.errorNotification,
        clearNotifications,
        labels
    };
}
