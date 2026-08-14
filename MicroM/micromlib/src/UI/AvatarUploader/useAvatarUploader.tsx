import { Text, useComponentDefaultProps } from "@mantine/core";
import { useCallback, useMemo } from "react";
import { MicroMClient } from "../../client";
import { EntityColumn } from "../../Entity";
import { ConfirmAndExecutePanel, useModal } from "../Core";
import { BrowserImageProcessingOptions, UploadProgressReport, useFilesUploadForm, useFileUpload } from "../FileUploader";
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
        client, fileProcessColumn, labels: suppliedLabels, initialImageURL, parentFormAPI,
        fileGUIDColumn, maxFileSize, editor, imageProcessing
    } = useComponentDefaultProps('AvatarUploader', AvatarUploaderDefaultProps, props);

    const labels = useMemo(
        () => ({ ...AvatarUploaderDefaultLabels, ...suppliedLabels }),
        [suppliedLabels]
    );

    const modals = useModal();
    const imageFileUploadOpen = useFilesUploadForm();

    const fileSizeProps = useMemo(() => maxFileSize === undefined
        ? {}
        : { maxIndividualFileSize: maxFileSize, maxTotalFilesSize: maxFileSize }, [maxFileSize]);

    const confirmReplacement = useCallback(async () => {
        return await new Promise<boolean>(async resolveConfirmation => {
            let settled = false;

            const settle = async (result: boolean) => {
                if (settled) return;
                settled = true;
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
                    onOK={() => settle(true)}
                    onCancel={() => settle(false)}
                />,
                modalProps: {
                    title: <Text fw="700">{labels.replaceTitle}</Text>,
                    size: 'sm',
                    closeOnClickOutside: false,
                    closeOnEscape: false,
                    withCloseButton: false
                },
                onClosed: () => {
                    if (settled) return;
                    settled = true;
                    resolveConfirmation(false);
                }
            });
        });
    }, [labels, modals]);

    const commitUploadedFile = useCallback(async (report: UploadProgressReport) => {
        if (!report.vc_fileguid) {
            return { error: true, message: 'The upload did not return a file GUID.' };
        }

        // EntityColumn is an intentional mutable model object shared with the parent form.
        // eslint-disable-next-line react-hooks/immutability
        fileGUIDColumn.value = report.vc_fileguid;
        return { error: false };
    }, [fileGUIDColumn]);

    const uploadAPI = useFileUpload({
        client,
        fileProcessColumn,
        maxFilesCount: 1,
        ...fileSizeProps,
        editor,
        imageProcessing,
        imageEditorTitle: labels.editorTitle,
        imageEditorProps: {
            saveLabel: labels.saveLabel,
            cancelLabel: labels.cancelLabel,
            rotateClockwiseLabel: labels.rotateClockwiseLabel,
            rotateCounterClockwiseLabel: labels.rotateCounterClockwiseLabel
        },
        imageEditorModalProps: {
            closeOnClickOutside: false,
            closeOnEscape: false,
            withCloseButton: false
        },
        onBeforeReplace: async () => await confirmReplacement(),
        onUploadComplete: commitUploadedFile
    });

    const selectedGUID = fileGUIDColumn.value || undefined;
    const currentFile = uploadAPI.files.find(file => file.vc_fileguid === selectedGUID);
    const fileGUID = currentFile?.vc_fileguid ?? selectedGUID;

    const imageURL = currentFile?.documentURL
        ?? (fileGUID ? client.getDocumentURL(fileGUID) : initialImageURL);

    const thumbnailURL = currentFile?.thumbnailURL
        ?? (fileGUID ? client.getThumbnailURL(fileGUID) : undefined);

    const handleOpenFileUpload = useCallback(async () => {
        await imageFileUploadOpen({
            fileProcessColumn,
            client,
            uploadAPI,
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
                editImageLabel: labels.editLabel,
                replaceExistingFile: true
            },
            filesUploadFormProps: {
                maxFilesCount: 1,
                ...fileSizeProps,
                editor: editor === true
            }
        });
    }, [client, editor, fileProcessColumn, fileSizeProps, imageFileUploadOpen, labels.editLabel, labels.modalTitle, uploadAPI]);

    const handleEditImage = useCallback(async () => {
        if (!currentFile?.vc_fileguid) return;
        await uploadAPI.editImage(currentFile.vc_fileguid);
    }, [currentFile?.vc_fileguid, uploadAPI]);

    const handleDeleteFile = useCallback(async (guid: string) => {
        if (!guid) return;

        const result = await uploadAPI.deleteFile(guid);
        if (result.Failed) return;

        // EntityColumn instances are intentional mutable models shared with the parent form.
        // eslint-disable-next-line react-hooks/immutability
        fileGUIDColumn.value = '';
        // eslint-disable-next-line react-hooks/immutability
        fileProcessColumn.value = '';
    }, [fileGUIDColumn, fileProcessColumn, uploadAPI]);

    return {
        imageURL,
        thumbnailURL,
        fileProcessID: uploadAPI.fileProcessID || undefined,
        fileGUID,
        handleOpenFileUpload,
        handleEditImage,
        handleDeleteFile,
        parentFormAPI,
        canEditImage: !!currentFile && uploadAPI.canEditImage(currentFile),
        processing: uploadAPI.processing,
        errorNotification: uploadAPI.errorNotification,
        clearNotifications: uploadAPI.clearNotifications!,
        labels
    };
}
