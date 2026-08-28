import { Alert, Stack } from "@mantine/core";
import { IconAlertTriangle } from "@tabler/icons-react";
import { FilesUploadForm, FileUploaderDefaultProps, UploadCompletionResult, UploadProgressReport, ValidateFileReturnType } from "../../UI/FileUploader";
import { ImportDataMappingEditor, ImportDataMappingState } from "../../UI/ImportData";
import { ImportEntityData } from "./ImportEntityData";

export interface ImportFileStepProps {
    entity: ImportEntityData,
    selectedFile: File | null,
    destinations: readonly string[],
    requiredDestinations: readonly string[],
    disabled?: boolean,
    validationError?: string,
    onValidateFile: (file: File) => Promise<ValidateFileReturnType>,
    onDelete: (fileGUID: string) => boolean | Promise<boolean>,
    onUploadComplete: (report: UploadProgressReport) => Promise<UploadCompletionResult | void>,
    onMappingChange: (state: ImportDataMappingState | undefined) => void,
}

export function ImportFileStep({
    entity, selectedFile, destinations, requiredDestinations, disabled, validationError,
    onValidateFile, onDelete, onUploadComplete, onMappingChange
}: ImportFileStepProps) {
    return (
        <Stack spacing="sm">
            {validationError &&
                <Alert color="yellow" icon={<IconAlertTriangle size="1rem" />}>{validationError}</Alert>
            }
            <FilesUploadForm
                fileProcessColumn={entity.def.columns.c_fileprocess_id}
                client={entity.API.client}
                maxFilesCount={1}
                uploaderProps={{
                    ...FileUploaderDefaultProps,
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
            {selectedFile &&
                <ImportDataMappingEditor
                    key={`${selectedFile.name}-${selectedFile.size}-${selectedFile.lastModified}`}
                    file={selectedFile}
                    destinations={destinations}
                    requiredDestinations={requiredDestinations}
                    onChange={onMappingChange}
                />
            }
        </Stack>
    );
}
