import { Text, useComponentDefaultProps } from "@mantine/core";
import { ReactNode, useCallback, useEffect, useMemo, useRef, useState, useSyncExternalStore } from "react";
import { DBStatusResult, MicroMClient, ValuesObject } from "../../client";
import { FileStoreClient } from "../../DataDictionary/FileStoreClient/FileStoreClient";
import { FileStoreProcess } from "../../DataDictionary/FileStoreProcess/FileStoreProcess";
import { convertRecordsToArrayOfValuesObject, EntityColumn } from "../../Entity";
import { MicroMModalSettings, useModal } from "../Core";
import { ImageEditor, ImageEditorProps } from "../ImageEditor";
import { BrowserImageProcessingOptions, getImageOutputSettings, getSupportedImageMimeType, isSupportedImageFile, processImageFileAutomatically, resolveImageProcessingOptions } from "../ImageEditor/imageProcessing";

export interface UploadProgressReport {
    errorMessage?: string,
    status_id: string,
    progress: number,
    done?: boolean,
    cancelled?: boolean,
    documentURL?: string,
    file_name: string,
    file_size: number,
    vc_fileguid?: string,
    thumbnailURL?: string,
}

export interface UseFileUploadSnapshot {
    files: readonly UploadProgressReport[],
    fileProcessID: string,
    maxIndividualFileSize?: number,
    maxTotalFilesSize?: number,
    maxFilesCount?: number,
    errorNotification?: string,
    cancelledNotification?: boolean,
    uploadingNotification?: boolean,
    loadingNotification?: boolean,
    processing: boolean,
    editorEnabled: boolean,
}

interface UseFileUploadActions {
    uploadFiles: (selectedFiles: File[]) => Promise<UploadProgressReport[]>,
    replaceFile: (fileGUID: string, replacementFile: File) => Promise<UploadProgressReport | null>,
    editImage: (file: string | UploadProgressReport) => Promise<UploadProgressReport | null>,
    openImageEditor: (file?: File) => Promise<File | null>,
    captureImage: () => Promise<UploadProgressReport | null>,
    canEditImage: (file: string | UploadProgressReport) => boolean,
    refreshFiles: () => Promise<Record<string, UploadProgressReport>>,
    deleteFile: (fileGUID: string) => Promise<DBStatusResult>,
    downloadFile: (fileUrl: string, fileName?: string) => Promise<void>,
    clearNotifications: () => void,
    cancelUpload: () => void,
}

export interface UseFileUploadReturnType extends UseFileUploadSnapshot, UseFileUploadActions {
    subscribe: (listener: () => void) => () => void,
    getSnapshot: () => UseFileUploadSnapshot,
}

export function useFileUploadSnapshot(uploadAPI: UseFileUploadReturnType) {
    return useSyncExternalStore(uploadAPI.subscribe, uploadAPI.getSnapshot, uploadAPI.getSnapshot);
}

export type ValidateFileReturnType = { error: boolean, message?: string };

export type UploadCompletionResult = ValidateFileReturnType;

export type ImageEditorConfigurationProps = Partial<Pick<ImageEditorProps,
    'saveLabel' | 'cancelLabel' | 'rotateClockwiseLabel' | 'rotateCounterClockwiseLabel' |
    'takePhotoLabel' | 'camera'>>;

export interface UseFileUploadProps {
    client: MicroMClient,
    fileProcessColumn: EntityColumn<string>,
    maxIndividualFileSize?: number,
    maxTotalFilesSize?: number,
    maxFilesCount?: number,
    youCanUploadAMaximumOfText?: string,
    filesText?: string,
    exceedMaximumIndividualSizeText?: string,
    unspecifiedErrorWhenUploadingFileText?: string,
    totalUploadExceedsMaximumSizeText?: string,
    onCancel?: () => void,
    editor?: boolean,
    imageProcessing?: BrowserImageProcessingOptions,
    imageEditorTitle?: ReactNode,
    imageEditorProps?: ImageEditorConfigurationProps,
    imageEditorModalProps?: MicroMModalSettings,
    onValidateFile?: (file: File) => Promise<ValidateFileReturnType>,
    onProcessFile?: (file: File) => Promise<File | null>,
    onBeforeUpload?: (file: File) => Promise<boolean>,
    onBeforeReplace?: (currentFile: UploadProgressReport, replacementFile?: File) => Promise<boolean>,
    onUploadComplete?: (report: UploadProgressReport) => Promise<UploadCompletionResult | void>,
    onDeleteComplete?: (fileGUID: string) => Promise<void> | void,
    thumbnailMaxSize?: number,
    thumbnailQuality?: number,
    loadFilesOnMount?: boolean,
}

export const UseFileUploadDefaultProps: Partial<UseFileUploadProps> = {
    maxIndividualFileSize: 2 * (1024 ** 2),
    maxTotalFilesSize: 10 * (1024 ** 2),
    maxFilesCount: 5,
    youCanUploadAMaximumOfText: 'You can upload a maximum of',
    filesText: 'files',
    exceedMaximumIndividualSizeText: 'exceeds maximum individual size of',
    unspecifiedErrorWhenUploadingFileText: 'Unspecified error when uploading file',
    totalUploadExceedsMaximumSizeText: 'Total upload exceeds maximum size of',
    thumbnailMaxSize: 150,
    thumbnailQuality: 75,
    loadFilesOnMount: true,
    editor: false,
    imageEditorTitle: 'Editor'
}

export type UploadStatus = 'Pending' | 'Uploading' | 'Uploaded' | 'Failed' | 'Cancelled';
type UploadOperation = 'idle' | 'loading' | 'uploading' | 'editing' | 'deleting';

const persistedFileToProgressReport = (file: ValuesObject, client: MicroMClient, thumbnailMaxSize?: number, thumbnailQuality?: number): UploadProgressReport => {
    const guid = String(file.vc_fileguid ?? '');
    const status = String(file.c_fileuploadstatus_id ?? '');

    const common = {
        status_id: guid,
        file_name: String(file.vc_filename ?? ''),
        file_size: Number(file.bi_filesize ?? 0),
        vc_fileguid: guid
    };

    switch (status as UploadStatus) {
        case 'Pending':
        case 'Uploading':
            return { ...common, progress: 0 };
        case 'Uploaded':
            return {
                ...common,
                progress: 100,
                done: true,
                documentURL: client.getDocumentURL(guid),
                thumbnailURL: client.getThumbnailURL(guid, thumbnailMaxSize, thumbnailQuality)
            };
        case 'Failed':
            return { ...common, progress: 0, done: true, errorMessage: 'Upload failed' };
        case 'Cancelled':
            return { ...common, progress: 0, done: true, cancelled: true };
        default:
            return {
                ...common,
                progress: 0,
                done: true,
                errorMessage: `Unknown upload status: ${status || '(empty)'}`
            };
    }
};

const isSuccessfulFile = (file: UploadProgressReport) =>
    file.done === true && !file.errorMessage && !file.cancelled && !!file.vc_fileguid;

const createStatusID = (file: File) => {
    const randomPart = typeof crypto !== 'undefined' && crypto.randomUUID
        ? crypto.randomUUID()
        : `${Date.now()}-${Math.random().toString(36).slice(2)}`;
    return `${file.name}-${file.size}-${randomPart}`;
};

export function useFileUpload(props: UseFileUploadProps): UseFileUploadReturnType {
    const {
        client, maxIndividualFileSize, maxTotalFilesSize, maxFilesCount,
        youCanUploadAMaximumOfText, filesText, exceedMaximumIndividualSizeText,
        unspecifiedErrorWhenUploadingFileText, totalUploadExceedsMaximumSizeText,
        fileProcessColumn, onCancel, editor, imageProcessing, imageEditorTitle,
        imageEditorProps, imageEditorModalProps, onValidateFile, onProcessFile,
        onBeforeUpload, onBeforeReplace, onUploadComplete, onDeleteComplete, thumbnailMaxSize,
        thumbnailQuality, loadFilesOnMount
    } = useComponentDefaultProps('useFileUpload', UseFileUploadDefaultProps, props);

    const [filesByID, setFilesByID] = useState<Record<string, UploadProgressReport>>({});
    const [operation, setOperation] = useState<UploadOperation>('idle');
    const [errorNotification, setErrorNotification] = useState<string>();
    const [cancelledNotification, setCancelledNotification] = useState<boolean>();

    const operationRef = useRef<UploadOperation>('idle');
    const activeControllerRef = useRef<AbortController>();
    const editorProcessedFilesRef = useRef(new WeakSet<File>());
    const modals = useModal();

    const files = useMemo(() => Object.values(filesByID), [filesByID]);

    const processingOptions = useMemo(() => resolveImageProcessingOptions(editor ? {
        ...imageProcessing,
        crop: imageProcessing?.crop ?? true,
        manualRotation: imageProcessing?.manualRotation ?? true
    } : imageProcessing), [editor, imageProcessing]);

    const beginOperation = useCallback((nextOperation: Exclude<UploadOperation, 'idle'>) => {
        if (operationRef.current !== 'idle') return undefined;

        const controller = new AbortController();
        operationRef.current = nextOperation;
        activeControllerRef.current = controller;
        setOperation(nextOperation);
        return controller;
    }, []);

    const finishOperation = useCallback((controller: AbortController) => {
        if (activeControllerRef.current !== controller) return;
        activeControllerRef.current = undefined;
        operationRef.current = 'idle';
        setOperation('idle');
    }, []);

    const setReport = useCallback((previousID: string, report: UploadProgressReport) => {
        const nextID = report.vc_fileguid || report.status_id || previousID;
        const nextReport = { ...report, status_id: nextID };

        setFilesByID(previous => {
            const next = { ...previous };
            delete next[previousID];
            next[nextID] = nextReport;
            return next;
        });

        return nextReport;
    }, []);

    const removeReport = useCallback((fileGUID: string) => {
        setFilesByID(previous => {
            const next = { ...previous };
            const key = Object.keys(next).find(id => id === fileGUID || next[id].vc_fileguid === fileGUID);
            if (key) delete next[key];
            return next;
        });
    }, []);

    const loadFilesFromServer = useCallback(async (signal?: AbortSignal) => {
        if (!fileProcessColumn.value) {
            setFilesByID({});
            return {};
        }

        const fileStore = new FileStoreClient(client);

        const data = await fileStore.API.executeView(
            fileStore.def.views.fcc_brwFiles,
            { c_fileprocess_id: fileProcessColumn.value },
            null,
            null,
            signal
        );

        const persistedFiles = data.flatMap(result => convertRecordsToArrayOfValuesObject(result, null));
        const refreshed: Record<string, UploadProgressReport> = {};

        persistedFiles.forEach(file => {
            const report = persistedFileToProgressReport(file, client, thumbnailMaxSize, thumbnailQuality);
            if (report.vc_fileguid) refreshed[report.vc_fileguid] = report;
        });

        setFilesByID(refreshed);
        return refreshed;
    }, [client, fileProcessColumn, thumbnailMaxSize, thumbnailQuality]);

    const refreshFiles = useCallback(async () => {
        const controller = beginOperation('loading');
        if (!controller) return filesByID;

        try {
            return await loadFilesFromServer(controller.signal);
        }
        catch (error: unknown) {
            if (!(error instanceof Error && error.name === 'AbortError')) {
                setErrorNotification(error instanceof Error ? error.message : String(error));
            }
            return filesByID;
        }
        finally {
            finishOperation(controller);
        }
    }, [beginOperation, filesByID, finishOperation, loadFilesFromServer]);

    useEffect(() => {
        if (!loadFilesOnMount) return;

        const controller = beginOperation('loading');
        if (!controller) return;

        void loadFilesFromServer(controller.signal)
            .catch((error: unknown) => {
                if (!(error instanceof Error && error.name === 'AbortError')) {
                    setErrorNotification(error instanceof Error ? error.message : String(error));
                }
            })
            .finally(() => finishOperation(controller));

        return () => {
            controller.abort();
            if (activeControllerRef.current === controller) {
                activeControllerRef.current = undefined;
                operationRef.current = 'idle';
            }
        };
    }, [beginOperation, fileProcessColumn.value, finishOperation, loadFilesFromServer, loadFilesOnMount]);

    useEffect(() => () => activeControllerRef.current?.abort(), []);

    const openImageEditor = useCallback(async (file?: File) => {
        if (!editor) return null;

        if (file) getImageOutputSettings(file, processingOptions);

        return await new Promise<File | null>(async resolveEditor => {
            let settled = false;

            const settle = async (result: File | null) => {
                if (settled) return;
                settled = true;
                await modals.close();
                resolveEditor(result);
            };

            await modals.open({
                content: <ImageEditor
                    sourceFile={file}
                    options={processingOptions}
                    {...imageEditorProps}
                    onSave={result => settle(result)}
                    onCancel={() => settle(null)}
                />,
                modalProps: {
                    trapFocus: true,
                    returnFocus: true,
                    title: typeof imageEditorTitle === 'string'
                        ? <Text fw="700">{imageEditorTitle}</Text>
                        : imageEditorTitle,
                    size: 'lg',
                    ...imageEditorModalProps
                },
                onClosed: () => {
                    if (settled) return;
                    settled = true;
                    resolveEditor(null);
                }
            });
        });
    }, [editor, imageEditorModalProps, imageEditorProps, imageEditorTitle, modals, processingOptions]);

    const processSelectedFile = useCallback(async (file: File) => {
        if (editorProcessedFilesRef.current.has(file)) {
            editorProcessedFilesRef.current.delete(file);
            return onProcessFile ? await onProcessFile(file) : file;
        }
        if (onProcessFile) return await onProcessFile(file);
        if (editor) return await openImageEditor(file);
        if (imageProcessing) return await processImageFileAutomatically(file, processingOptions);
        return file;
    }, [editor, imageProcessing, onProcessFile, openImageEditor, processingOptions]);

    const validateFileSize = useCallback((file: File, excludedGUIDs: readonly string[] = []) => {
        if (file.size > maxIndividualFileSize!) {
            setErrorNotification(`"${file.name}" ${exceedMaximumIndividualSizeText} ${maxIndividualFileSize! / (1024 ** 2)}MB`);
            return false;
        }

        const uploadedSize = files
            .filter(item => isSuccessfulFile(item) && !excludedGUIDs.includes(item.vc_fileguid!))
            .reduce((total, item) => total + item.file_size, 0);

        if ((uploadedSize + file.size) > maxTotalFilesSize!) {
            setErrorNotification(`${totalUploadExceedsMaximumSizeText} ${maxTotalFilesSize! / (1024 ** 2)}MB`);
            return false;
        }

        return true;
    }, [exceedMaximumIndividualSizeText, files, maxIndividualFileSize, maxTotalFilesSize, totalUploadExceedsMaximumSizeText]);

    const prepareSelectedFile = useCallback(async (file: File, excludedGUIDs: readonly string[] = []) => {
        if (onValidateFile) {
            const validation = await onValidateFile(file);
            if (validation.error) {
                setErrorNotification(validation.message || '');
                return null;
            }
        }

        const processedFile = await processSelectedFile(file);
        if (!processedFile || !validateFileSize(processedFile, excludedGUIDs)) return null;

        if (onBeforeUpload && !await onBeforeUpload(processedFile)) return null;
        return processedFile;
    }, [onBeforeUpload, onValidateFile, processSelectedFile, validateFileSize]);

    const uploadFile = useCallback(async (file: File, signal: AbortSignal) => {
        const statusID = createStatusID(file);

        if (signal.aborted) {
            return {
                status_id: statusID,
                file_name: file.name,
                file_size: file.size,
                progress: 0,
                done: true,
                cancelled: true
            } as UploadProgressReport;
        }

        try {
            let currentProcessID = fileProcessColumn.value;

            if (!currentProcessID) {
                const fileProcess = new FileStoreProcess(client);
                await fileProcess.API.addData(signal);
                currentProcessID = fileProcess.def.columns.c_fileprocess_id.value;
                // EntityColumn is an intentional mutable model object shared with the parent form.
                // eslint-disable-next-line react-hooks/immutability
                fileProcessColumn.value = currentProcessID;
            }

            const result = await client.upload(
                file,
                currentProcessID,
                signal,
                thumbnailMaxSize,
                thumbnailQuality,
                (_, progress) => setReport(statusID, {
                    status_id: statusID,
                    file_name: file.name,
                    file_size: file.size,
                    progress
                })
            );

            const errorMessage = result?.ErrorMessage
                || (!result?.vc_fileguid ? 'The upload did not return a file GUID.' : undefined);
            return setReport(statusID, {
                file_name: file.name,
                file_size: file.size,
                status_id: result?.vc_fileguid || statusID,
                progress: errorMessage ? 0 : 100,
                done: true,
                errorMessage,
                documentURL: result?.documentURL,
                thumbnailURL: result?.thumbnailURL,
                vc_fileguid: result?.vc_fileguid
            });
        }
        catch (error: unknown) {
            const cancelled = error instanceof Error && error.name === 'AbortError';
            return setReport(statusID, {
                file_name: file.name,
                file_size: file.size,
                status_id: statusID,
                done: true,
                progress: 0,
                cancelled,
                errorMessage: cancelled ? undefined : error instanceof Error ? error.message : String(error)
            });
        }
    }, [client, fileProcessColumn, setReport, thumbnailMaxSize, thumbnailQuality]);

    const completeUpload = useCallback(async (report: UploadProgressReport) => {
        if (!onUploadComplete || !report.vc_fileguid) return true;

        try {
            const completion = await onUploadComplete(report);
            if (!completion?.error) return true;

            setErrorNotification(completion.message || unspecifiedErrorWhenUploadingFileText);
            return false;
        }
        catch (error: unknown) {
            setErrorNotification(error instanceof Error ? error.message : String(error));
            return false;
        }
    }, [onUploadComplete, unspecifiedErrorWhenUploadingFileText]);

    const uploadFiles = useCallback(async (selectedFiles: File[]) => {
        const reports: UploadProgressReport[] = [];
        const controller = beginOperation('uploading');
        if (!controller) return reports;

        try {
            const countedFiles = files.filter(file => !file.errorMessage && !file.cancelled).length;
            if ((countedFiles + selectedFiles.length) > maxFilesCount!) {
                setErrorNotification(`${youCanUploadAMaximumOfText} ${maxFilesCount} ${filesText}.`);
                return reports;
            }

            setErrorNotification(undefined);
            setCancelledNotification(false);

            let projectedFiles = [...files];
            for (const selectedFile of selectedFiles) {
                let file: File;
                try {
                    const preparedFile = await prepareSelectedFile(selectedFile);
                    if (!preparedFile) continue;
                    file = preparedFile;
                }
                catch (error: unknown) {
                    setErrorNotification(error instanceof Error ? error.message : String(error));
                    continue;
                }

                const projectedSize = projectedFiles.filter(isSuccessfulFile)
                    .reduce((total, item) => total + item.file_size, 0);
                if ((projectedSize + file.size) > maxTotalFilesSize!) {
                    setErrorNotification(`${totalUploadExceedsMaximumSizeText} ${maxTotalFilesSize! / (1024 ** 2)}MB`);
                    continue;
                }

                const result = await uploadFile(file, controller.signal);
                reports.push(result);

                if (result.cancelled) {
                    setCancelledNotification(true);
                    break;
                }
                if (result.errorMessage) {
                    setErrorNotification(result.errorMessage);
                    break;
                }

                projectedFiles = [...projectedFiles, result];
                if (!await completeUpload(result)) break;
            }

            return reports;
        }
        finally {
            finishOperation(controller);
        }
    }, [beginOperation, completeUpload, files, filesText, finishOperation, maxFilesCount, maxTotalFilesSize, prepareSelectedFile, totalUploadExceedsMaximumSizeText, uploadFile, youCanUploadAMaximumOfText]);

    const deleteStoredFile = useCallback(async (fileGUID: string, signal: AbortSignal) => {
        const fileStore = new FileStoreClient(client);
        fileStore.def.columns.vc_fileguid.value = fileGUID;

        const result = await fileStore.API.deleteData(undefined, signal);

        if (!result.Failed) removeReport(fileGUID);
        return result;
    }, [client, removeReport]);

    const deleteFile = useCallback(async (fileGUID: string) => {
        const controller = beginOperation('deleting');
        if (!controller) return { Failed: true } as DBStatusResult;

        try {
            setErrorNotification(undefined);
            const result = await deleteStoredFile(fileGUID, controller.signal);
            if (!result.Failed) await onDeleteComplete?.(fileGUID);
            return result;
        }
        catch (error: unknown) {
            if (!(error instanceof Error && error.name === 'AbortError')) {
                setErrorNotification(error instanceof Error ? error.message : String(error));
            }
            return { Failed: true } as DBStatusResult;
        }
        finally {
            finishOperation(controller);
        }
    }, [beginOperation, deleteStoredFile, finishOperation, onDeleteComplete]);

    const findFile = useCallback((file: string | UploadProgressReport) => {
        if (typeof file !== 'string') return file;
        return files.find(item => item.vc_fileguid === file || item.status_id === file);
    }, [files]);

    const canEditImage = useCallback((file: string | UploadProgressReport) => {
        const report = findFile(file);
        return editor === true && !!report?.vc_fileguid && !!report.documentURL
            && isSupportedImageFile(report.file_name);
    }, [editor, findFile]);

    const performReplacement = useCallback(async (currentFile: UploadProgressReport, replacementSource: File, signal: AbortSignal) => {
        const currentGUID = currentFile.vc_fileguid!;
        const preparedFile = await prepareSelectedFile(replacementSource, [currentGUID]);
        if (!preparedFile) return null;

        const replacement = await uploadFile(preparedFile, signal);
        if (replacement.cancelled) {
            setCancelledNotification(true);
            return null;
        }
        if (replacement.errorMessage || !replacement.vc_fileguid) {
            setErrorNotification(replacement.errorMessage || unspecifiedErrorWhenUploadingFileText);
            return null;
        }

        const rollbackReplacement = async () => {
            const recoveryController = new AbortController();

            try {
                const rollback = await deleteStoredFile(replacement.vc_fileguid!, recoveryController.signal);
                if (rollback.Failed) throw new Error('Rollback delete failed');
                setErrorNotification('The existing image could not be replaced. The current image was kept.');
            }
            catch {
                try { await loadFilesFromServer(); } catch { }
                setErrorNotification('The existing image could not be replaced and the rollback state could not be verified.');
            }
        };

        let oldFileDeleted = false;
        try {
            oldFileDeleted = !(await deleteStoredFile(currentGUID, signal)).Failed;
        }
        catch {
            try {
                const recovered = await loadFilesFromServer();
                const oldFileStillExists = !!recovered[currentGUID];
                const replacementExists = !!recovered[replacement.vc_fileguid];

                if (!oldFileStillExists && replacementExists) {
                    oldFileDeleted = true;
                }
                else if (oldFileStillExists) {
                    if (replacementExists) await rollbackReplacement();
                    else setErrorNotification('The existing image could not be replaced. The current image was kept.');
                    return null;
                }
                else {
                    setErrorNotification('The replacement state could not be verified after deleting the existing image.');
                    return null;
                }
            }
            catch {
                setErrorNotification('The replacement state could not be verified after deleting the existing image.');
                return null;
            }
        }

        if (!oldFileDeleted) {
            await rollbackReplacement();
            return null;
        }

        try {
            const refreshed = await loadFilesFromServer(signal);
            if (!refreshed[replacement.vc_fileguid] || refreshed[currentGUID]) {
                setErrorNotification('The replacement was uploaded, but the refreshed file list is inconsistent.');
                return null;
            }
        }
        catch (error: unknown) {
            setErrorNotification(`The replacement was uploaded, but the file list could not be refreshed: ${error instanceof Error ? error.message : String(error)}`);
            return null;
        }

        return await completeUpload(replacement) ? replacement : null;
    }, [completeUpload, deleteStoredFile, loadFilesFromServer, prepareSelectedFile, unspecifiedErrorWhenUploadingFileText, uploadFile]);

    const replaceFile = useCallback(async (fileGUID: string, replacementFile: File) => {
        const currentFile = findFile(fileGUID);
        if (!currentFile?.vc_fileguid) return null;

        const controller = beginOperation('uploading');
        if (!controller) return null;

        try {
            setErrorNotification(undefined);
            setCancelledNotification(false);
            if (onBeforeReplace && !await onBeforeReplace(currentFile, replacementFile)) return null;
            return await performReplacement(currentFile, replacementFile, controller.signal);
        }
        catch (error: unknown) {
            if (!(error instanceof Error && error.name === 'AbortError')) {
                setErrorNotification(error instanceof Error ? error.message : String(error));
            }
            return null;
        }
        finally {
            finishOperation(controller);
        }
    }, [beginOperation, findFile, finishOperation, onBeforeReplace, performReplacement]);

    const editImage = useCallback(async (file: string | UploadProgressReport) => {
        const currentFile = findFile(file);
        if (!currentFile || !canEditImage(currentFile)) return null;

        const controller = beginOperation('editing');
        if (!controller) return null;

        try {
            setErrorNotification(undefined);
            setCancelledNotification(false);
            if (onBeforeReplace && !await onBeforeReplace(currentFile)) return null;

            const blob = await client.downloadBlob(currentFile.documentURL!, controller.signal);
            const imageType = getSupportedImageMimeType(currentFile.file_name, blob.type);
            if (!imageType) {
                throw new Error(`The image format "${blob.type || currentFile.file_name}" cannot be processed in the browser.`);
            }

            const sourceFile = new File([blob], currentFile.file_name, {
                type: imageType,
                lastModified: Date.now()
            });
            return await performReplacement(currentFile, sourceFile, controller.signal);
        }
        catch (error: unknown) {
            if (!(error instanceof Error && error.name === 'AbortError')) {
                setErrorNotification(error instanceof Error ? error.message : String(error));
            }
            return null;
        }
        finally {
            finishOperation(controller);
        }
    }, [beginOperation, canEditImage, client, findFile, finishOperation, onBeforeReplace, performReplacement]);

    const captureImage = useCallback(async () => {
        const capturedFile = await openImageEditor();
        if (!capturedFile) return null;

        editorProcessedFilesRef.current.add(capturedFile);
        const reports = await uploadFiles([capturedFile]);
        return reports.find(report => !!report.vc_fileguid && !report.errorMessage && !report.cancelled) ?? null;
    }, [openImageEditor, uploadFiles]);

    const downloadFile = useCallback(async (fileUrl: string, fileName?: string) => {
        const now = new Date();
        const datePart = now.toISOString().split('T')[0];
        const fileExtension = fileUrl.split('?')[0].split('.').pop() || 'file';
        const filename = fileName || `download-${datePart}.${fileExtension}`;

        try {
            const blob = await client.downloadBlob(fileUrl);
            if (!window.Blob || !window.URL) {
                alert('Your browser does not support downloading this file.');
                return;
            }

            const blobUrl = URL.createObjectURL(blob);
            const linkElement = document.createElement('a');
            linkElement.setAttribute('download', filename);
            linkElement.setAttribute('href', blobUrl);
            linkElement.click();
            setTimeout(() => URL.revokeObjectURL(blobUrl), 100);
        }
        catch (error: unknown) {
            setErrorNotification(error instanceof Error ? error.message : String(error));
        }
    }, [client]);

    const clearNotifications = useCallback(() => {
        setErrorNotification(undefined);
        setCancelledNotification(false);
    }, []);

    const cancelUpload = useCallback(() => {
        activeControllerRef.current?.abort();
        onCancel?.();
    }, [onCancel]);

    const uploadingNotification = operation === 'uploading' || operation === 'editing';
    const loadingNotification = operation === 'loading' || operation === 'deleting';

    const snapshot = useMemo<UseFileUploadSnapshot>(() => ({
        files,
        fileProcessID: fileProcessColumn.value,
        maxFilesCount,
        maxIndividualFileSize,
        maxTotalFilesSize,
        errorNotification,
        cancelledNotification,
        uploadingNotification,
        loadingNotification,
        processing: operation !== 'idle',
        editorEnabled: editor === true
    }), [cancelledNotification, editor, errorNotification, fileProcessColumn.value, files, loadingNotification, maxFilesCount, maxIndividualFileSize, maxTotalFilesSize, operation, uploadingNotification]);

    const snapshotRef = useRef(snapshot);
    const subscribersRef = useRef(new Set<() => void>());
    snapshotRef.current = snapshot;

    useEffect(() => {
        subscribersRef.current.forEach(listener => listener());
    }, [snapshot]);

    const subscribe = useCallback((listener: () => void) => {
        subscribersRef.current.add(listener);
        return () => subscribersRef.current.delete(listener);
    }, []);
    const getSnapshot = useCallback(() => snapshotRef.current, []);

    const actionsRef = useRef<UseFileUploadActions>();
    actionsRef.current = {
        uploadFiles,
        replaceFile,
        editImage,
        openImageEditor,
        captureImage,
        canEditImage,
        refreshFiles,
        deleteFile,
        downloadFile,
        clearNotifications,
        cancelUpload
    };

    const stableActions = useMemo<UseFileUploadActions>(() => ({
        uploadFiles: selectedFiles => actionsRef.current!.uploadFiles(selectedFiles),
        replaceFile: (fileGUID, replacementFile) => actionsRef.current!.replaceFile(fileGUID, replacementFile),
        editImage: file => actionsRef.current!.editImage(file),
        openImageEditor: file => actionsRef.current!.openImageEditor(file),
        captureImage: () => actionsRef.current!.captureImage(),
        canEditImage: file => actionsRef.current!.canEditImage(file),
        refreshFiles: () => actionsRef.current!.refreshFiles(),
        deleteFile: fileGUID => actionsRef.current!.deleteFile(fileGUID),
        downloadFile: (fileUrl, fileName) => actionsRef.current!.downloadFile(fileUrl, fileName),
        clearNotifications: () => actionsRef.current!.clearNotifications(),
        cancelUpload: () => actionsRef.current!.cancelUpload()
    }), []);

    return {
        ...stableActions,
        ...snapshot,
        subscribe,
        getSnapshot
    };
}
