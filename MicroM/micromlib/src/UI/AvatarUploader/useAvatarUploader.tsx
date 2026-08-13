import { Text, useComponentDefaultProps } from "@mantine/core";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { MicroMClient } from "../../client";
import { FileStoreClient } from "../../DataDictionary/FileStoreClient/FileStoreClient";
import { EntityColumn } from "../../Entity";
import { ConfirmAndExecutePanel, useModal } from "../Core";
import { UploadProgressReport, useFilesUploadForm, useFileUpload } from "../FileUploader";
import { ImageEditor } from "../FileUploader/ImageEditor";
import { BrowserImageProcessingOptions, getImageOutputSettings, getSupportedImageMimeType, processImageFileAutomatically, resolveImageProcessingOptions } from "../FileUploader/imageProcessing";
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
    fileProcessID?: string,
    fileGUID?: string,
    handleOpenFileUpload: () => Promise<void>,
    handleEditImage: () => Promise<void>,
    handleDeleteFile: (fileGUID: string) => Promise<void>,
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
        fileProcessColumn,
        loadFilesOnMount: false
    });

    const [imageURL, setImageURL] = useState<string | undefined>(initialImageURL);
    const [thumbnailURL, setThumbnailURL] = useState<string>();
    const [fileProcessID, setFileProcessID] = useState<string>();
    const [fileGUID, setFileGUID] = useState<string>();
    const [editing, setEditing] = useState(false);
    const [localError, setLocalError] = useState<string>();
    const currentFile = useRef<{ fileGUID?: string }>({});
    const replacementSnapshot = useRef<string[]>([]);

    currentFile.current = { fileGUID };

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
        setFileProcessID(fileProcessColumn.value);

        return { error: false, removeFromQueue };
    }, [client, fileGUIDColumn, fileProcessColumn]);

    const snapshotProcessFiles = useCallback(async () => {
        if (!fileProcessColumn.value) {
            replacementSnapshot.current = [];
            return [];
        }

        const files = await new FileStoreClient(client).listFiles(fileProcessColumn.value);
        const guids = files.map(file => file.vc_fileguid).filter(Boolean);
        if (currentFile.current.fileGUID && !guids.includes(currentFile.current.fileGUID)) {
            guids.push(currentFile.current.fileGUID);
        }
        replacementSnapshot.current = [...new Set(guids)];
        return files;
    }, [client, fileProcessColumn]);

    const replaceAndCommit = useCallback(async (report: UploadProgressReport, removeFromQueue: boolean) => {
        const replacementGUID = report.vc_fileguid;
        const displayedGUID = currentFile.current.fileGUID;

        if (!replacementGUID || replacementGUID === displayedGUID) {
            return { error: true, message: 'The replacement upload did not return a new file GUID.' };
        }

        const snapshot = replacementSnapshot.current.filter(guid => guid !== replacementGUID);
        const deletionOrder = [
            ...snapshot.filter(guid => guid !== displayedGUID),
            ...(displayedGUID && snapshot.includes(displayedGUID) ? [displayedGUID] : [])
        ];
        const fileStore = new FileStoreClient(client);

        for (const guid of deletionOrder) {
            try {
                const deletion = await fileStore.deleteFile(guid);
                if (deletion.Failed) throw new Error('Delete failed');
            }
            catch {
                try { await fileStore.deleteFile(replacementGUID); } catch { }
                return { error: true, message: 'The existing image could not be replaced. The current image was kept.' };
            }
        }

        replacementSnapshot.current = [];
        await commitUploadedFile(report, removeFromQueue);

        try {
            const refreshed = await fileStore.listFiles(fileProcessColumn.value);
            if (refreshed.length !== 1 || refreshed[0].vc_fileguid !== replacementGUID) {
                return { error: true, message: 'The replacement was uploaded, but the refreshed avatar file list is inconsistent.' };
            }
        }
        catch (e: unknown) {
            return {
                error: true,
                message: `The replacement was uploaded, but the avatar file list could not be refreshed: ${e instanceof Error ? e.message : String(e)}`
            };
        }

        return { error: false, removeFromQueue };
    }, [client, commitUploadedFile, fileProcessColumn]);

    const directUploadAPI = useFileUpload({
        client,
        maxFilesCount: 1,
        ...fileSizeProps,
        fileProcessColumn,
        loadFilesOnMount: false,
        editor: editorEnabled,
        onProcessFile: processFile,
        onUploadComplete: async report => await replaceAndCommit(report, true)
    });

    const handleOpenFileUpload = useCallback(async () => {

        await imageFileUploadOpen({
            fileProcessColumn,
            client,
            modalTitle: labels.modalTitle,
            onOK: () => { },
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
                loadFilesOnMount: false,
                editor: editorEnabled,
                onProcessFile: processFile,
                onBeforeUpload: async () => {
                    await snapshotProcessFiles();
                    return await confirmReplacement();
                },
                onUploadComplete: async report => await replaceAndCommit(report, false)
            }
        });

    }, [client, confirmReplacement, editorEnabled, fileProcessColumn, fileSizeProps, imageFileUploadOpen, labels.editLabel, labels.modalTitle, processFile, replaceAndCommit, snapshotProcessFiles]);

    const handleEditImage = useCallback(async () => {
        if (!editorEnabled || !imageURL || !fileGUID || editing) return;

        setEditing(true);
        setLocalError(undefined);
        try {
            const files = await snapshotProcessFiles();
            if (!await confirmReplacement()) return;

            const source = files.find(file => file.vc_fileguid === fileGUID);
            const blob = await client.downloadBlob(client.getDocumentURL(fileGUID));
            const fileName = source?.vc_filename || fileGUID;
            const imageType = getSupportedImageMimeType(fileName, blob.type);
            if (!imageType) throw new Error(`The image format "${blob.type || fileName}" cannot be processed in the browser.`);

            await directUploadAPI.uploadFiles([new File([blob], fileName, {
                type: imageType,
                lastModified: Date.now()
            })]);
        }
        catch (e: unknown) {
            setLocalError(e instanceof Error ? e.message : String(e));
        }
        finally {
            setEditing(false);
        }
    }, [client, confirmReplacement, directUploadAPI, editing, editorEnabled, fileGUID, imageURL, snapshotProcessFiles]);

    const handleDeleteFile = useCallback(async (guid: string) => {
        if (!guid) return;

        const result = await deletionAPI.deleteFile(guid);
        if (result.Failed) return;

        fileGUIDColumn.value = '';
        fileProcessColumn.value = '';

        setFileProcessID(undefined);
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
        setLocalError(undefined);
        directUploadAPI.clearNotifications?.();
        deletionAPI.clearNotifications?.();
    }, [deletionAPI, directUploadAPI]);

    return {
        imageURL,
        thumbnailURL,
        fileProcessID,
        fileGUID,
        handleOpenFileUpload,
        handleEditImage,
        handleDeleteFile,
        parentFormAPI,
        canEditImage: editorEnabled && !!imageURL && !!fileGUID,
        processing: editing || !!directUploadAPI.uploadingNotification || !!directUploadAPI.loadingNotification || !!deletionAPI.loadingNotification,
        errorNotification: localError || directUploadAPI.errorNotification || deletionAPI.errorNotification,
        clearNotifications,
        labels
    };
}
