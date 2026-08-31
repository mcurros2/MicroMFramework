import { Alert, Stack } from "@mantine/core";
import { IconAlertTriangle } from "@tabler/icons-react";
import { FilesUploadForm, FilesUploadFormProps, UploadCompletionResult, UploadProgressReport, ValidateFileReturnType } from "../../UI/FileUploader";
import { ImportEntityData } from "./ImportEntityData";

export type ImportFileStepCustomizationProps = Omit<FilesUploadFormProps,
    'client' | 'fileProcessColumn' | 'maxFilesCount' | 'onDelete' | 'onOK' | 'onUploadComplete' |
    'onValidateFile' | 'showOKButton' | 'uploaderProps' | 'uploadAPI'> & {
        uploaderProps?: Omit<NonNullable<FilesUploadFormProps['uploaderProps']>, 'accept' | 'disabled'>,
    };

export interface ImportFileStepProps extends ImportFileStepCustomizationProps {
    entity: ImportEntityData,
    disabled?: boolean,
    validationError?: string,
    onValidateFile: (file: File) => Promise<ValidateFileReturnType>,
    onDelete: (fileGUID: string) => boolean | Promise<boolean>,
    onUploadComplete: (report: UploadProgressReport) => Promise<UploadCompletionResult | void>,
}

export function ImportFileStep({
    entity, disabled, validationError, onValidateFile, onDelete, onUploadComplete, ...rest
}: ImportFileStepProps) {
    const { uploaderProps, ...filesUploadFormProps } = rest;

    return (
        <Stack spacing="sm">
            {validationError &&
                <Alert color="yellow" icon={<IconAlertTriangle size="1rem" />}>{validationError}</Alert>
            }
            <FilesUploadForm
                {...filesUploadFormProps}
                fileProcessColumn={entity.def.columns.c_fileprocess_id}
                client={entity.API.client}
                maxFilesCount={1}
                uploaderProps={{
                    ...uploaderProps,
                    disabled,
                    accept: [
                        '.csv',
                        '.xls',
                        '.xlsx',
                        'text/csv',
                        'application/vnd.ms-excel',
                        'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
                    ]
                }}
                onValidateFile={onValidateFile}
                onDelete={onDelete}
                onUploadComplete={onUploadComplete}
            />
        </Stack>
    );
}
