import { Text } from "@mantine/core";
import { ModalSettings } from "@mantine/modals/lib/context";
import { useCallback, useRef } from "react";
import { EntityColumn } from "../../Entity";
import { MicroMClient } from "../../client";
import { useModal } from "../Core";
import { FileUploaderProps } from "./FileUploader";
import { FilesUploadForm, FilesUploadFormProps } from "./FilesUploadForm";
import { UploadProgressReport } from "./useFileUpload";

export interface ModalFileUploadProps {
    client: MicroMClient,
    fileProcessColumn: EntityColumn<string>,
    onOK?: (fileprocess_id: string, uploadProgress: Record<string, UploadProgressReport>) => void,
    onCancel?: () => void,
    onClosed?: () => void,
    modalProps?: ModalSettings,
    modalTitle?: string,
    filesUploadFormProps: Omit<FilesUploadFormProps, 'fileProcessColumn' | 'uploaderProps' | 'client' | 'onOK' | 'onCancel'>,
    uploaderProps?: Omit<FileUploaderProps, 'uploadAPI' | 'abortSignal'>
}

export const UseFileUploadFormOpenDefaultProps: Partial<ModalFileUploadProps> = {
    modalProps: { size: 'xl' },
    modalTitle: 'Upload Files'
}
export function useFilesUploadForm() {
    const modals = useModal();
    const buttonResult = useRef<'OK' | 'Cancel' | 'Quit'>('Quit');

    const open = useCallback(async (props: ModalFileUploadProps) => {
        const {
            client, onOK, onCancel, modalProps, onClosed, modalTitle, uploaderProps, fileProcessColumn, filesUploadFormProps
        } = { ...UseFileUploadFormOpenDefaultProps, ...props };

        buttonResult.current = 'Quit';

        const handleOK = async (fileprocess_id: string, uploadProgress: Record<string, UploadProgressReport>) => {
            buttonResult.current = 'OK';
            await modals.close();
            if (onOK) {
                await onOK(fileprocess_id, uploadProgress);
            }
        };

        const handleCancel = async () => {
            buttonResult.current = 'Cancel';
            await modals.close();
            if (onCancel) {
                onCancel();
            }
        };

        const handleClosed = async () => {
            if (buttonResult.current === 'Quit') {
                if (onCancel) {
                    onCancel();
                }
            }
            if (onClosed) onClosed();
        }

        await modals.open(
            {
                content:
                    <FilesUploadForm
                        fileProcessColumn={fileProcessColumn}
                        uploaderProps={uploaderProps}
                        client={client}
                        onOK={async (fileprocess_id: string, uploadProgress: Record<string, UploadProgressReport>) => await handleOK(fileprocess_id, uploadProgress)}
                        onCancel={() => handleCancel()}
                        showOKButton
                        {...filesUploadFormProps}
                    />,
                modalProps: {
                    ...modalProps,
                    trapFocus: true,
                    returnFocus: true,
                    title: <Text fw="700">{modalTitle}</Text>,
                },
                onClosed: async () => {
                    await handleClosed();
                }

            });

    }, [modals]);

    return open;

}