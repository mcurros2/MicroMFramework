import { Text, useComponentDefaultProps } from "@mantine/core";
import { useEffect, useRef, useState } from "react";
import { DBStatusResult, MicroMClient } from "../../client";
import { FileStore } from "../../DataDictionary";
import { FileStoreProcess } from "../../DataDictionary/FileStoreProcess/FileStoreProcess";
import { EntityColumn, mapDataResultToType } from "../../Entity";
import { useModal } from "../Core";
import { ImageEditor } from "./ImageEditor";
import { resolveImageProcessingOptions } from "./imageProcessing";

export interface UseFileUploadReturnType {
    uploadFiles: (selectedFiles: File[]) => Promise<UploadProgressReport[]>,
    deleteFile: (file_id: string, fileGUID: string) => Promise<DBStatusResult>,
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
    rollbackUploadedFile?: boolean,
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
    editor?: 'none' | 'image',
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
}

export interface UploadProgressReport {
    errorMessage?: string,
    status_id: string,
    file_id?: string,
    progress: number,
    done?: boolean,
    cancelled?: boolean,
    documentURL?: string,
    file_name: string,
    file_size: number,
    vc_fileguid?: string,
    thumbnailURL?: string,
}

export type UploadStatus = 'Pending' | 'Uploading' | 'Uploaded' | 'Failed' | 'Cancelled';

export type FileUploaderView = {
    c_file_id: string;
    vc_filename: string;
    vc_filefolder: string;
    vc_fileguid: string;
    c_fileuploadstatus_id: string;
    bi_filesize: number;
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

    const abortController = new AbortController();
    const abort_signal = abortController.signal;

    const modals = useModal();

    // MMC: get existing uploaded files for the process
    useEffect(() => {

        const refreshFiles = async () => {
            setLoadingNotification(true);
            try {
                const fileStore = new FileStore(client);
                fileStore.def.columns.c_fileprocess_id.value = fileProcessColumn.value;
                const data = await fileStore.API.executeView(fileStore.def.views.fst_brwFiles);
                setLoadingNotification(false);
                if (data && data.length > 0) {
                    let totalSize = 0;
                    setUploadProgress((prev) => {
                        const updatedState = { ...prev };
                        const file_records = mapDataResultToType<FileUploaderView>(data[0], "enforceObject", null, null);

                        file_records.forEach((file) => {
                            updatedState[file.c_file_id] = {
                                file_id: file.c_file_id,
                                status_id: file.c_file_id,
                                file_name: file.vc_filename,
                                file_size: file.bi_filesize,
                                progress: 100,
                                done: true,
                                documentURL: client.getDocumentURL(file.vc_fileguid),
                                thumbnailURL: client.getThumbnailURL(file.vc_fileguid, thumbnailMaxSize, thumbnailQuality),
                                vc_fileguid: file.vc_fileguid
                            };
                            totalSize += file.bi_filesize;
                        });
                        return updatedState;
                    });
                    uploadedSize.current = totalSize;
                }
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

        if (loadFilesOnMount && fileProcessColumn.value) refreshFiles();

    }, [client, fileProcessColumn.value, loadFilesOnMount, thumbnailMaxSize, thumbnailQuality]);


    const openImageEditor = async (file: File) => {
        const source = URL.createObjectURL(file);
        const options = resolveImageProcessingOptions({ crop: true });

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
                    src={source}
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
                    URL.revokeObjectURL(source);
                    if (!closeHandled) {
                        closeHandled = true;
                        resolveEditor(null);
                    }
                }
            });
        });
    };

    const uploadFiles = async (selectedFiles: File[]) => {
        const reports: UploadProgressReport[] = [];

        // Check against maxFilesCount.
        if ((Object.keys(uploadProgress).length + selectedFiles.length) > maxFilesCount!) {
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
                const processedFile = onProcessFile
                    ? await onProcessFile(file)
                    : editor === 'image' ? await openImageEditor(file) : file;
                if (!processedFile) continue;
                file = processedFile;
            }
            catch (e: unknown) {
                setErrorNotification(e instanceof Error ? e.message : String(e));
                continue;
            }

            // Size limits apply to the final file that will be uploaded.
            if (file.size > maxIndividualFileSize!) {
                setErrorNotification(`"${file.name}" ${exceedMaximumIndividualSizeText} ${maxIndividualFileSize! / (1024 ** 2)}MB`);
                continue;
            }

            if ((uploadedSize.current + file.size) > maxTotalFilesSize!) {
                setErrorNotification(`${totalUploadExceedsMaximumSizeText} ${maxTotalFilesSize! / (1024 ** 2)}MB`);
                break;
            }

            if (onBeforeUpload && !await onBeforeUpload(file)) {
                continue;
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

            if (onUploadComplete && result.file_id && result.vc_fileguid) {
                try {
                    const completion = await onUploadComplete(result);
                    if (completion?.error) {
                        let message = completion.message || unspecifiedErrorWhenUploadingFileText;
                        let rollbackFailed = false;
                        if (completion.rollbackUploadedFile) {
                            const rollback = await deleteFile(result.file_id, result.vc_fileguid);
                            rollbackFailed = rollback.Failed;
                            if (rollbackFailed) message += ' The replacement upload could not be removed.';
                        }
                        if (!completion.rollbackUploadedFile || rollbackFailed) {
                            result.errorMessage = message;
                            result.progress = 0;
                            updateProgress(result.status_id, result);
                        }
                        setErrorNotification(message);
                        break;
                    }
                    if (completion?.removeFromQueue) {
                        setUploadProgress(prev => {
                            const updated = { ...prev };
                            delete updated[result.file_id!];
                            return updated;
                        });
                        uploadedSize.current = Math.max(0, uploadedSize.current - result.file_size);
                    }
                }
                catch (e: unknown) {
                    const message = e instanceof Error ? e.message : String(e);
                    setErrorNotification(message);
                    break;
                }
            }
        }

        setUploadingNotification(false);
        return reports;
    }

    const clearNotifications = () => { setErrorNotification(''); setCancelledNotification(false); }

    const updateProgress = (statusId: string, newStatus: UploadProgressReport) => {
        setUploadProgress((prev) => {
            if (newStatus.file_id) {
                const updatedState = { ...prev };

                // MMC: delete previous status
                delete updatedState[newStatus.status_id];

                // MMC: change status_id to file_id (this will allow to upload the same file several times)
                newStatus.status_id = newStatus.file_id;

                // MMC: add new status
                updatedState[newStatus.file_id] = newStatus;

                return updatedState;
            } else {
                return { ...prev, [statusId]: newStatus };
            }
        });
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
                // Update the total uploaded size and check against maxTotalFilesSize.
                uploadedSize.current += file.size;

                const finalResult: UploadProgressReport = {
                    file_id: result.FileId,
                    file_name: file.name,
                    file_size: file.size,
                    status_id: status_id,
                    progress: result.ErrorMessage ? 0 : 100,
                    done: true,
                    errorMessage: result.ErrorMessage,
                    documentURL: result.documentURL,
                    thumbnailURL: result.thumbnailURL,
                    vc_fileguid: result.FileGuid
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

    const deleteFile = async (file_id: string, fileGUID: string) => {
        try {
            setLoadingNotification(true);
            const fileStore = new FileStore(client);
            fileStore.def.columns.c_file_id.value = file_id;
            fileStore.def.columns.vc_fileguid.value = fileGUID;
            const result = await fileStore.API.deleteData(undefined, abort_signal);
            setLoadingNotification(false);
            if (!result.Failed) {
                const deletedFileSize = uploadProgress[file_id]?.file_size || 0;
                setUploadProgress((prev) => {
                    const updatedState = { ...prev };
                    delete updatedState[file_id];
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
