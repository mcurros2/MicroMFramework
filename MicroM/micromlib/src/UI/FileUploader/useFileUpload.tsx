import { Text, useComponentDefaultProps } from "@mantine/core";
import { useCallback, useEffect, useRef, useState } from "react";
import { DBStatusResult, MicroMClient, ValuesObject } from "../../client";
import { FileStoreClient } from "../../DataDictionary/FileStoreClient/FileStoreClient";
import { FileStoreProcess } from "../../DataDictionary/FileStoreProcess/FileStoreProcess";
import { convertRecordsToArrayOfValuesObject, EntityColumn } from "../../Entity";
import { useModal } from "../Core";
import { ImageEditor } from "./ImageEditor";
import { getSupportedImageMimeType, resolveImageProcessingOptions } from "./imageProcessing";

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

export interface UseFileUploadReturnType {
    uploadFiles: (selectedFiles: File[]) => Promise<UploadProgressReport[]>,
    editImage: (report: UploadProgressReport) => Promise<UploadProgressReport | null>,
    deleteFile: (fileGUID: string) => Promise<DBStatusResult>,
    downloadFile: (fileUrl: string, fileName?: string) => Promise<void>,
    uploadProgress: Record<string, UploadProgressReport>,
    fileProcessID: string,
    maxIndividualFileSize?: number,
    maxTotalFilesSize?: number,
    maxFilesCount?: number,
    errorNotification?: string,
    cancelledNotification?: boolean,
    uploadingNotification?: boolean,
    clearNotifications?: () => void,
    cancelUpload?: () => void,
    loadingNotification?: boolean,
}

export type ValidateFileReturnType = { error: boolean, message?: string };

export type UploadCompletionResult = ValidateFileReturnType & {
    removeFromQueue?: boolean,
};

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
    onValidateFile?: (file: File) => Promise<ValidateFileReturnType>,
    onProcessFile?: (file: File) => Promise<File | null>,
    onBeforeUpload?: (file: File) => Promise<boolean>,
    onUploadComplete?: (report: UploadProgressReport) => Promise<UploadCompletionResult | void>,
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
}

export type UploadStatus = 'Pending' | 'Uploading' | 'Uploaded' | 'Failed' | 'Cancelled';

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
            return { ...common, progress: 0 };
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

export function useFileUpload(props: UseFileUploadProps): UseFileUploadReturnType {
    const {
        client, maxIndividualFileSize, maxTotalFilesSize, maxFilesCount,
        youCanUploadAMaximumOfText, filesText, exceedMaximumIndividualSizeText, unspecifiedErrorWhenUploadingFileText,
        totalUploadExceedsMaximumSizeText, fileProcessColumn, onCancel, editor, onValidateFile,
        onProcessFile, onBeforeUpload, onUploadComplete,
        thumbnailMaxSize, thumbnailQuality, loadFilesOnMount
    } = useComponentDefaultProps('useFileUpload', UseFileUploadDefaultProps, props);

    const [uploadProgress, setUploadProgress] = useState<Record<string, UploadProgressReport>>({});
    const [fileProcessID, setFileProcessID] = useState<string>(fileProcessColumn.value);

    const uploadedSize = useRef<number>(0);

    const [loadingNotification, setLoadingNotification] = useState<boolean>();
    const [errorNotification, setErrorNotification] = useState<string>();
    const [cancelledNotification, setCancelledNotification] = useState<boolean>();
    const [uploadingNotification, setUploadingNotification] = useState<boolean>();

    const [abortController] = useState(() => new AbortController());
    const abort_signal = abortController.signal;

    const modals = useModal();

    const refreshFiles = useCallback(async () => {
        if (!fileProcessColumn.value) {
            setUploadProgress({});
            uploadedSize.current = 0;
            return {} as Record<string, UploadProgressReport>;
        }

        const fileStore = new FileStoreClient(client);
        const data = await fileStore.API.executeView(
            fileStore.def.views.fcc_brwFiles,
            { c_fileprocess_id: fileProcessColumn.value },
            null,
            null,
            abort_signal
        );
        const files = data.flatMap(result => convertRecordsToArrayOfValuesObject(result, null));
        const refreshed: Record<string, UploadProgressReport> = {};
        let totalSize = 0;

        files.forEach(file => {
            const report = persistedFileToProgressReport(file, client, thumbnailMaxSize, thumbnailQuality);
            if (!report.vc_fileguid) return;

            refreshed[report.vc_fileguid] = report;
            if (file.c_fileuploadstatus_id === 'Uploaded') totalSize += report.file_size;
        });

        setUploadProgress(refreshed);
        uploadedSize.current = totalSize;
        return refreshed;
    }, [abort_signal, client, fileProcessColumn, thumbnailMaxSize, thumbnailQuality]);

    // MMC: get existing uploaded files for the process
    useEffect(() => {
        const loadFiles = async () => {
            setLoadingNotification(true);
            try {
                await refreshFiles();
                setLoadingNotification(false);
            }
            catch (e: unknown) {
                setLoadingNotification(false);
                if (e instanceof Error) {
                    if (e.name !== 'AbortError') {
                        setErrorNotification(e.message);
                    }
                } else {
                    setErrorNotification(String(e));
                }
            }

        };

        if (loadFilesOnMount && fileProcessColumn.value) loadFiles();

    }, [fileProcessColumn.value, loadFilesOnMount, refreshFiles]);


    const openImageEditor = async (file: File) => {
        const options = resolveImageProcessingOptions({ crop: true, manualRotation: true });

        return await new Promise<File | null>(async resolveEditor => {
            let closeHandled = false;

            const close = async (result: File | null) => {
                if (closeHandled) return;
                closeHandled = true;
                resolveEditor(result);
                await modals.close();
            };

            await modals.open({
                content: <ImageEditor
                    sourceFile={file}
                    options={options}
                    onSave={async result => await close(result)}
                    onCancel={async () => await close(null)}
                />,
                modalProps: {
                    trapFocus: true,
                    returnFocus: true,
                    title: <Text fw="700">Editor</Text>,
                    size: 'lg'
                },
                onClosed: () => {
                    if (!closeHandled) {
                        closeHandled = true;
                        resolveEditor(null);
                    }
                }
            });
        });
    };

    const processSelectedFile = async (file: File) => {
        return onProcessFile
            ? await onProcessFile(file)
            : editor ? await openImageEditor(file) : file;
    };

    const validateFileSize = (file: File, replacedFileSize = 0) => {
        if (file.size > maxIndividualFileSize!) {
            setErrorNotification(`"${file.name}" ${exceedMaximumIndividualSizeText} ${maxIndividualFileSize! / (1024 ** 2)}MB`);
            return false;
        }

        if ((uploadedSize.current - replacedFileSize + file.size) > maxTotalFilesSize!) {
            setErrorNotification(`${totalUploadExceedsMaximumSizeText} ${maxTotalFilesSize! / (1024 ** 2)}MB`);
            return false;
        }

        return true;
    };

    const uploadFiles = async (selectedFiles: File[]) => {
        const reports: UploadProgressReport[] = [];
        const countedFiles = Object.values(uploadProgress).filter(report => !report.errorMessage && !report.cancelled).length;

        // Check against maxFilesCount.
        if ((countedFiles + selectedFiles.length) > maxFilesCount!) {
            setErrorNotification(`${youCanUploadAMaximumOfText} ${maxFilesCount} ${filesText}.`);
            return reports;
        }

        setErrorNotification('');
        setCancelledNotification(false);
        setUploadingNotification(true);

        for (let file of selectedFiles) {
            // Client-side validation
            if (onValidateFile) {
                const validation = await onValidateFile(file);
                if (validation.error) {
                    setErrorNotification(validation.message || '');
                    continue;
                }
            }

            try {
                const processedFile = await processSelectedFile(file);
                if (!processedFile) continue;
                file = processedFile;
            }
            catch (e: unknown) {
                setErrorNotification(e instanceof Error ? e.message : String(e));
                continue;
            }

            if (!validateFileSize(file)) continue;

            if (onBeforeUpload) {
                try {
                    if (!await onBeforeUpload(file)) continue;
                }
                catch (e: unknown) {
                    setErrorNotification(e instanceof Error ? e.message : String(e));
                    continue;
                }
            }

            const result = await uploadFile(file);
            reports.push(result);
            if (result.errorMessage && !result.cancelled) {
                setErrorNotification(result.errorMessage);
                break;
            }
            if (result.cancelled) {
                setCancelledNotification(true);
                break;
            }

            if (!await completeUpload(result)) break;
        }

        setUploadingNotification(false);
        return reports;
    }

    const clearNotifications = () => { setErrorNotification(''); setCancelledNotification(false); }

    const updateProgress = (statusId: string, newStatus: UploadProgressReport) => {
        setUploadProgress((prev) => {
            if (newStatus.vc_fileguid) {
                const updatedState = { ...prev };

                // MMC: delete previous status
                delete updatedState[newStatus.status_id];

                // Completed uploads are keyed only by their public GUID.
                newStatus.status_id = newStatus.vc_fileguid;

                // MMC: add new status
                updatedState[newStatus.vc_fileguid] = newStatus;

                return updatedState;
            } else {
                return { ...prev, [statusId]: newStatus };
            }
        });
    };

    const completeUpload = async (report: UploadProgressReport) => {
        if (!onUploadComplete || !report.vc_fileguid) return true;

        try {
            const completion = await onUploadComplete(report);
            if (completion?.error) {
                const message = completion.message || unspecifiedErrorWhenUploadingFileText;
                report.errorMessage = message;
                report.progress = 0;
                updateProgress(report.status_id, report);
                setErrorNotification(message);
                return false;
            }
            if (completion?.removeFromQueue) {
                setUploadProgress(prev => {
                    const updated = { ...prev };
                    delete updated[report.vc_fileguid!];
                    return updated;
                });
                uploadedSize.current = Math.max(0, uploadedSize.current - report.file_size);
            }
            return true;
        }
        catch (e: unknown) {
            setErrorNotification(e instanceof Error ? e.message : String(e));
            return false;
        }
    };

    const uploadFile = async (file: File) => {
        if (abort_signal.aborted) return { cancelled: true } as UploadProgressReport;

        const status_id = `${file.name}-${file.size}-${file.lastModified}`;

        try {
            let currentProcessID = fileProcessColumn.value;

            // MMC: create process_id (file group) if not already created
            if (!currentProcessID) {
                setLoadingNotification(true);
                const fileProcess = new FileStoreProcess(client);
                await fileProcess.API.addData(abort_signal);

                currentProcessID = fileProcess.def.columns.c_fileprocess_id.value;
                setFileProcessID(currentProcessID);
                // EntityColumn is an intentional mutable model object shared with the parent form.
                // eslint-disable-next-line react-hooks/immutability
                fileProcessColumn.value = currentProcessID;
                setLoadingNotification(false);
            }

            // MMC: the upload endpoint will update the file_store data
            const result = await client.upload(file, currentProcessID, abort_signal, thumbnailMaxSize, thumbnailQuality, (file, progress) => {
                updateProgress(status_id, {
                    status_id: status_id,
                    file_name: file.name,
                    file_size: file.size,
                    progress: progress
                });
            });

            // MMC: update finished status
            if (result) {
                const errorMessage = result.ErrorMessage
                    || (!result.vc_fileguid ? 'The upload did not return a file GUID.' : undefined);
                if (!errorMessage) uploadedSize.current += file.size;

                const finalResult: UploadProgressReport = {
                    file_name: file.name,
                    file_size: file.size,
                    status_id: status_id,
                    progress: errorMessage ? 0 : 100,
                    done: true,
                    errorMessage,
                    documentURL: result.documentURL,
                    thumbnailURL: result.thumbnailURL,
                    vc_fileguid: result.vc_fileguid
                };
                updateProgress(status_id, finalResult);
                return finalResult;
            }
        }
        catch (error: unknown) {
            setLoadingNotification(false);
            const errorResult: UploadProgressReport = {
                file_name: file.name,
                file_size: file.size,
                status_id: status_id,
                done: true,
                progress: 0,
                errorMessage: error instanceof Error ? error.message : String(error)
            };
            updateProgress(status_id, errorResult);
            return errorResult;
        }

        return {
            status_id: status_id,
            progress: 0,
            done: true,
            errorMessage: unspecifiedErrorWhenUploadingFileText
        } as UploadProgressReport;
    }

    const deleteFile = async (fileGUID: string) => {
        try {
            setLoadingNotification(true);
            const fileStore = new FileStoreClient(client);
            fileStore.def.columns.vc_fileguid.value = fileGUID;
            const result = await fileStore.API.deleteData(undefined, abort_signal);

            setLoadingNotification(false);
            if (!result.Failed) {
                const deletedFileSize = uploadProgress[fileGUID]?.file_size || 0;
                setUploadProgress((prev) => {
                    const updatedState = { ...prev };
                    delete updatedState[fileGUID];
                    return updatedState;
                });
                uploadedSize.current = Math.max(0, uploadedSize.current - deletedFileSize);
            };
            return result;
        }
        catch (e: unknown) {
            setLoadingNotification(false);
            if (e instanceof Error) {
                if (e.name !== 'AbortError') {
                    setErrorNotification(e.message);
                }
            } else {
                setErrorNotification(String(e));
            }
            return { Failed: true } as DBStatusResult;
        }
    }

    const editImage = async (report: UploadProgressReport) => {
        if (!editor || !report.vc_fileguid || !report.documentURL) return null;

        setErrorNotification('');
        setCancelledNotification(false);
        setUploadingNotification(true);

        try {
            const blob = await client.downloadBlob(report.documentURL);
            const imageType = getSupportedImageMimeType(report.file_name, blob.type);
            if (!imageType) {
                throw new Error(`The image format "${blob.type || report.file_name}" cannot be processed in the browser.`);
            }

            let file = new File([blob], report.file_name, {
                type: imageType,
                lastModified: Date.now()
            });

            if (onValidateFile) {
                const validation = await onValidateFile(file);
                if (validation.error) {
                    setErrorNotification(validation.message || '');
                    return null;
                }
            }

            const processedFile = await processSelectedFile(file);
            if (!processedFile) return null;
            file = processedFile;

            const replacedFileSize = report.file_size;
            if (!validateFileSize(file, replacedFileSize)) return null;
            if (onBeforeUpload && !await onBeforeUpload(file)) return null;

            const replacement = await uploadFile(file);
            if (replacement.errorMessage) {
                if (!replacement.cancelled) setErrorNotification(replacement.errorMessage);
                return null;
            }
            if (replacement.cancelled || !replacement.vc_fileguid || replacement.vc_fileguid === report.vc_fileguid) {
                setCancelledNotification(true);
                return null;
            }

            const deletion = await deleteFile(report.vc_fileguid);
            if (deletion.Failed) {
                await deleteFile(replacement.vc_fileguid);
                setErrorNotification('The existing image could not be replaced. The current image was kept.');
                return null;
            }

            try {
                const refreshed = await refreshFiles();
                if (!refreshed[replacement.vc_fileguid] || refreshed[report.vc_fileguid]) {
                    setErrorNotification('The replacement was uploaded, but the refreshed file list is inconsistent.');
                    return null;
                }
            }
            catch (e: unknown) {
                setErrorNotification(`The replacement was uploaded, but the file list could not be refreshed: ${e instanceof Error ? e.message : String(e)}`);
                return null;
            }

            if (!await completeUpload(replacement)) return null;

            return replacement;
        }
        catch (e: unknown) {
            setErrorNotification(e instanceof Error ? e.message : String(e));
            return null;
        }
        finally {
            setUploadingNotification(false);
        }
    };


    const downloadFile = async (fileUrl: string, fileName?: string) => {
        const now = new Date();
        const datePart = now.toISOString().split('T')[0]; // "2023-11-02"

        const fileExtension = fileUrl.split('?')[0].split('.').pop() || 'file';

        const defaultFilename = `download-${datePart}.${fileExtension}`;
        const filename = fileName || defaultFilename;

        try {

            const blob = await client.downloadBlob(fileUrl);

            if (window.Blob && window.URL) {
                const blobUrl = URL.createObjectURL(blob);
                const linkElement = document.createElement('a');
                linkElement.setAttribute('download', filename);
                linkElement.setAttribute('href', blobUrl);
                linkElement.click();
                setTimeout(() => URL.revokeObjectURL(blobUrl), 100);
            } else {
                alert("Your browser does not support downloading this file.");
            }

        } catch (error) {
            console.error("There was an error downloading the file", error);
        }

    };

    const cancelUpload = async () => {
        abortController.abort();
        if (onCancel) onCancel();
    }

    return {
        uploadFiles,
        editImage,
        deleteFile,
        downloadFile,
        uploadProgress,
        fileProcessID,
        maxFilesCount,
        maxIndividualFileSize,
        maxTotalFilesSize,
        errorNotification,
        cancelledNotification,
        uploadingNotification,
        clearNotifications,
        cancelUpload,
        loadingNotification
    }
}
