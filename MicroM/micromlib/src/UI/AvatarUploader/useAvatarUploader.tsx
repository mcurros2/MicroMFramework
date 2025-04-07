import { useComponentDefaultProps } from "@mantine/core";
import { useCallback, useEffect, useState } from "react";
import { EntityColumn } from "../../Entity";
import { MicroMClient } from "../../client";
import { useFileUpload, useFilesUploadForm } from "../FileUploader";
import { UseEntityFormReturnType } from "../Form";

export interface useAvatarUploaderProps {
    client: MicroMClient,
    fileProcessColumn: EntityColumn<string>,
    fileGUIDColumn: EntityColumn<string>,
    initialImageURL?: string,
    labels?: AvatarUploaderLabels,
    parentFormAPI?: UseEntityFormReturnType
}

export type AvatarUploaderLabels = {
    modalTitle?: string,
}

export const AvatarUploaderDefaultProps: Partial<useAvatarUploaderProps> = {
    labels: { modalTitle: 'Upload Image' }
};

export interface AvatarUploaderAPI {
    imageURL?: string,
    fileID?: string,
    fileProcessID?: string,
    fileGUID?: string,
    handleOpenFileUpload: () => void,
    handleDeleteFile: (file_id: string, fileGUID: string) => void,
    parentFormAPI?: UseEntityFormReturnType
}

export function useAvatarUploader(props: useAvatarUploaderProps): AvatarUploaderAPI {
    const {
        client, fileProcessColumn, labels, initialImageURL, parentFormAPI, fileGUIDColumn
    } = useComponentDefaultProps('AvatarUploader', AvatarUploaderDefaultProps, props);

    const imageFileUploadOpen = useFilesUploadForm();

    const { deleteFile } = useFileUpload({
        client,
        fileProcessColumn: fileProcessColumn
    });

    const [imageURL, setImageURL] = useState<string | undefined>(initialImageURL);
    const [fileID, setFileID] = useState<string | undefined>(undefined);
    const [fileProcessID, setFileProcessID] = useState<string | undefined>(undefined);
    const [fileGUID, setFileGUID] = useState<string | undefined>(undefined);


    const handleOpenFileUpload = useCallback(async () => {
        await imageFileUploadOpen({
            fileProcessColumn: fileProcessColumn,
            client: client,
            modalTitle: labels?.modalTitle,
            onOK: (fileprocess_id, uploadProgress) => {
                fileProcessColumn.value = fileprocess_id;
                const keys = Object.keys(uploadProgress);
                if (keys.length > 0 && uploadProgress[keys[0]].vc_fileguid) {
                    fileGUIDColumn.value = uploadProgress[keys[0]].vc_fileguid!;
                    setFileGUID(fileGUIDColumn.value);
                    setImageURL(client.getDocumentURL(fileGUIDColumn.value));
                    setFileID(uploadProgress[keys[0]].file_id);
                    setFileProcessID(fileprocess_id);
                }
                else {
                    // MMC: if no file was uploaded, clear the file column
                    fileGUIDColumn.value = '';
                    setFileGUID(undefined);
                    setImageURL(undefined);
                    setFileID(undefined);
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
            },
            filesUploadFormProps: {
                maxFilesCount: 1,
            }
        });
    }, [client, fileGUIDColumn, fileProcessColumn, imageFileUploadOpen, labels?.modalTitle]);

    const handleDeleteFile = async (file_id: string, fileGUID: string) => {
        await deleteFile(file_id, fileGUID);
        fileGUIDColumn.value = '';
        fileProcessColumn.value = '';
        setFileID(undefined);
        setImageURL(undefined);
        setFileGUID(undefined);
    };

    // MMC: set the loaded URL when finishing the entity get
    useEffect(() => {
        if (parentFormAPI) {
            if (parentFormAPI.status.loading === false && parentFormAPI.status.operationType === 'get') {
                if (fileProcessColumn.value) {
                    setFileProcessID(fileProcessColumn.value);
                    setImageURL(client.getDocumentURL(fileGUIDColumn.value));
                    setFileGUID(fileGUIDColumn.value);
                }
            }
        }
    }, [client, parentFormAPI]);

    return {
        imageURL,
        fileID,
        fileProcessID,
        fileGUID,
        handleOpenFileUpload,
        handleDeleteFile,
        parentFormAPI
    }

}