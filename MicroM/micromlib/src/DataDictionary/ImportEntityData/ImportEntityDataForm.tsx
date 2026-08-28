import { Button, Card, Group, Stack, Table, Title, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconCheck, IconCircleX, IconX } from "@tabler/icons-react";
import { useCallback, useMemo, useRef, useState } from "react";
import { DBStatusResult, OperationStatus } from "../../client";
import { Entity, EntityDefinition, getRequiredColumns } from "../../Entity";
import { CircleFilledIcon } from "../../UI/Core/CircleFilledIcon";
import { FormOptions } from "../../UI/Core/types";
import { UploadCompletionResult, UploadProgressReport, ValidateFileReturnType } from "../../UI/FileUploader";
import { StepperForm, StepperFormStep, useEntityForm } from "../../UI/Form";
import { ImportDataMappingState, useImportData } from "../../UI/ImportData";
import { ImportEntityData } from "./ImportEntityData";
import { ImportFileStep } from "./ImportFileStep";
import { ImportInstructionsStep } from "./ImportInstructionsStep";
import { ImportSummaryStep } from "./ImportSummaryStep";

export interface ImportEntityDataFormProps extends FormOptions<ImportEntityData> {
    importEntity?: Entity<EntityDefinition>,
    entityProcName?: string,
    excludedImportDestinations?: string[],
    onImportSuccess?: () => Promise<void>,
    instructionsStepLabel?: string,
    instructionsStepDescription?: string,
    uploadStepLabel?: string,
    uploadStepDescription?: string,
    summaryStepLabel?: string,
    summaryStepDescription?: string,
    instructionsLabel?: string,
    columnHeaderLabel?: string,
    dataNameLabel?: string,
    contentLabel?: string,
    dataTypeLabel?: string,
    dateFormatLabel?: string,
    numberFormatLabel?: string,
    downloadExcelSampleLabel?: string,
    downloadCSVSampleLabel?: string,
    errorReadingFileLabel?: string,
    emptyFileLabel?: string,
    uploadRequiredLabel?: string,
    mappingRequiredLabel?: string,
    importButtonLabel?: string,
    importedFileLabel?: string,
    recordsImportedSuccessfullyLabel?: string,
    recordsNotImportedDueToErrorsLabel?: string,
    errorColumnTitle?: string,
    rowColumnTitle?: string,
}

export const ImportEntityDataFormDefaultProps: Partial<ImportEntityDataFormProps> = {
    initialFormMode: "add",
    instructionsStepLabel: "Instructions",
    instructionsStepDescription: "Review the required columns",
    uploadStepLabel: "Upload file",
    uploadStepDescription: "Upload and map columns",
    summaryStepLabel: "Summary",
    summaryStepDescription: "Review and import",
    instructionsLabel: "CSV and Excel files must include a header row. These columns can be mapped after selecting a file:",
    columnHeaderLabel: "Column header",
    dataNameLabel: "Data name",
    contentLabel: "Content",
    dataTypeLabel: "Data type",
    dateFormatLabel: "* Dates should use the format YYYY-MM-DD.",
    numberFormatLabel: "* Numbers with decimal places should use '.' as the separator.",
    downloadExcelSampleLabel: "Download Excel sample",
    downloadCSVSampleLabel: "Download CSV sample",
    errorReadingFileLabel: "Error reading file",
    emptyFileLabel: "The selected file is empty.",
    uploadRequiredLabel: "Wait for the selected file to finish uploading before continuing.",
    mappingRequiredLabel: "Map at least one file column before continuing.",
    importButtonLabel: "Import data",
    importedFileLabel: "Imported file",
    recordsImportedSuccessfullyLabel: "Records imported successfully",
    recordsNotImportedDueToErrorsLabel: "Records not imported due to errors",
    errorColumnTitle: "Error",
    rowColumnTitle: "Row",
}

interface ImportCompletedContentProps {
    fileName: string,
    result: NonNullable<ReturnType<typeof useImportData>['importStatus']['data']>,
    onClose?: () => void | Promise<void>,
    importedFileLabel?: string,
    recordsImportedSuccessfullyLabel?: string,
    recordsNotImportedDueToErrorsLabel?: string,
    errorColumnTitle?: string,
    rowColumnTitle?: string,
    closeLabel?: React.ReactNode,
}

function ImportCompletedContent({
    fileName, result, onClose, importedFileLabel, recordsImportedSuccessfullyLabel,
    recordsNotImportedDueToErrorsLabel, errorColumnTitle, rowColumnTitle, closeLabel
}: ImportCompletedContentProps) {
    const theme = useMantineTheme();

    return (
        <Stack>
            <Card withBorder>
                <Stack>
                    <Title order={6}>{importedFileLabel} {fileName}</Title>
                    {result.SuccessCount > 0 &&
                        <Group spacing={0}>
                            <CircleFilledIcon icon={<IconCheck size="0.75rem" />} backColor={theme.colors.green[8]} />
                            {result.SuccessCount} {recordsImportedSuccessfullyLabel}
                        </Group>
                    }
                    {result.ErrorCount > 0 &&
                        <Group spacing={0}>
                            <CircleFilledIcon icon={<IconX size="0.75rem" />} backColor={theme.colors.red[8]} />
                            {result.ErrorCount} {recordsNotImportedDueToErrorsLabel}
                        </Group>
                    }
                    {result.ErrorCount > 0 &&
                        <Table withBorder striped width="100%">
                            <thead><tr><th>{rowColumnTitle}</th><th>{errorColumnTitle}</th></tr></thead>
                            <tbody>
                                {Object.entries(result.Errors).slice(0, 10).map(([key, value]) => (
                                    <tr key={key}><td>{key}</td><td>{value}</td></tr>
                                ))}
                            </tbody>
                        </Table>
                    }
                </Stack>
            </Card>
            <Group position="right">
                <Button variant="light" leftIcon={<IconCircleX size="1.5rem" />} onClick={() => void onClose?.()}>
                    {closeLabel}
                </Button>
            </Group>
        </Stack>
    );
}

export function ImportEntityDataForm(props: ImportEntityDataFormProps) {
    const {
        entity, initialFormMode, getDataOnInit, onCancel, importEntity, entityProcName,
        excludedImportDestinations, onImportSuccess, instructionsStepLabel, instructionsStepDescription,
        uploadStepLabel, uploadStepDescription, summaryStepLabel, summaryStepDescription,
        instructionsLabel, columnHeaderLabel, dataNameLabel, contentLabel, dataTypeLabel,
        dateFormatLabel, numberFormatLabel, downloadExcelSampleLabel, downloadCSVSampleLabel,
        errorReadingFileLabel, emptyFileLabel, uploadRequiredLabel, mappingRequiredLabel,
        importButtonLabel, importedFileLabel, recordsImportedSuccessfullyLabel,
        recordsNotImportedDueToErrorsLabel, errorColumnTitle, rowColumnTitle, CancelText, CloseText
    } = useComponentDefaultProps('ImportEntityDataForm', ImportEntityDataFormDefaultProps, props);

    const importData = useImportData(importEntity);
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [uploadCompleted, setUploadCompleted] = useState(false);
    const [mappingState, setMappingState] = useState<ImportDataMappingState>();
    const [uploadValidationError, setUploadValidationError] = useState<string>();
    const importSuccessNotified = useRef(false);

    const requiredColumns = useMemo(
        () => importEntity?.def.importColumns || getRequiredColumns(importEntity),
        [importEntity]
    );

    const importDestinations = useMemo(() => {
        const entityProc = entityProcName ? importEntity?.def.procs[entityProcName] : undefined;
        const destinations = entityProcName ? Object.keys(entityProc?.parms ?? {}) : requiredColumns;
        const excluded = new Set((excludedImportDestinations ?? []).map(destination => destination.toLowerCase()));
        return destinations.filter(destination => !excluded.has(destination.toLowerCase()));
    }, [entityProcName, excludedImportDestinations, importEntity, requiredColumns]);

    const effectiveRequiredColumns = useMemo(() => {
        const allowed = new Set(importDestinations.map(destination => destination.toLowerCase()));
        return requiredColumns.filter(column => allowed.has(column.toLowerCase()));
    }, [importDestinations, requiredColumns]);

    const executeImport = useCallback(async (): Promise<OperationStatus<DBStatusResult>> => {
        const fileProcessID = entity.def.columns.c_fileprocess_id.value;
        if (!fileProcessID || !mappingState?.isValid) {
            return {
                error: { name: 'ImportMappingError', status: 400, message: mappingRequiredLabel || 'The import mapping is not ready.' },
                operationType: 'add'
            };
        }

        const result = await importData.execute(
            fileProcessID,
            entityProcName,
            mappingState.mapping,
            mappingState.initialRow
        );

        if (result?.error) return { error: result.error, operationType: 'add' };
        if (!result?.data) return { loading: false, operationType: 'add' };

        if (!importSuccessNotified.current) {
            importSuccessNotified.current = true;
            await onImportSuccess?.();
        }

        return {
            data: { Failed: false, AutonumReturned: false, Results: [{ Status: 0, Message: 'OK' }] },
            operationType: 'add'
        };
    }, [entity.def.columns.c_fileprocess_id.value, entityProcName, importData, mappingRequiredLabel, mappingState, onImportSuccess]);

    const formAPI = useEntityForm({
        entity,
        initialFormMode: initialFormMode === 'view' ? 'add' : initialFormMode,
        getDataOnInit: getDataOnInit!,
        onCancel,
        saveAndGetOverride: executeImport
    });

    const handleValidateFile = useCallback(async (file: File): Promise<ValidateFileReturnType> => {
        const extension = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();
        if (!['.csv', '.xls', '.xlsx'].includes(extension)) {
            return { error: true, message: errorReadingFileLabel };
        }
        if (file.size === 0) return { error: true, message: emptyFileLabel };

        setSelectedFile(file);
        setUploadCompleted(false);
        setMappingState(undefined);
        setUploadValidationError(undefined);
        importSuccessNotified.current = false;
        return { error: false };
    }, [emptyFileLabel, errorReadingFileLabel]);

    const handleDeleteFile = useCallback(() => {
        setSelectedFile(null);
        setUploadCompleted(false);
        setMappingState(undefined);
        setUploadValidationError(undefined);
        importSuccessNotified.current = false;
        return true;
    }, []);

    const handleUploadComplete = useCallback(async (_report: UploadProgressReport): Promise<UploadCompletionResult> => {
        setUploadCompleted(true);
        return { error: false };
    }, []);

    const validateUploadStep = useCallback(() => {
        if (!selectedFile || !uploadCompleted || !entity.def.columns.c_fileprocess_id.value) {
            setUploadValidationError(uploadRequiredLabel);
            return false;
        }
        if (!mappingState?.isValid) {
            setUploadValidationError(mappingRequiredLabel);
            return false;
        }
        setUploadValidationError(undefined);
        return true;
    }, [entity.def.columns.c_fileprocess_id.value, mappingRequiredLabel, mappingState?.isValid, selectedFile, uploadCompleted, uploadRequiredLabel]);

    const steps = useMemo<StepperFormStep[]>(() => [
        {
            name: 'instructions',
            label: instructionsStepLabel!,
            description: instructionsStepDescription,
            content: <ImportInstructionsStep
                importEntity={importEntity}
                requiredColumns={effectiveRequiredColumns}
                instructionsLabel={instructionsLabel}
                columnHeaderLabel={columnHeaderLabel}
                dataNameLabel={dataNameLabel}
                contentLabel={contentLabel}
                dataTypeLabel={dataTypeLabel}
                dateFormatLabel={dateFormatLabel}
                numberFormatLabel={numberFormatLabel}
                downloadExcelSampleLabel={downloadExcelSampleLabel}
                downloadCSVSampleLabel={downloadCSVSampleLabel}
            />
        },
        {
            name: 'upload',
            label: uploadStepLabel!,
            description: uploadStepDescription,
            nextStepValidation: validateUploadStep,
            content: <ImportFileStep
                entity={entity}
                selectedFile={selectedFile}
                destinations={importDestinations}
                requiredDestinations={effectiveRequiredColumns}
                disabled={formAPI.status.loading}
                validationError={uploadValidationError}
                onValidateFile={handleValidateFile}
                onDelete={handleDeleteFile}
                onUploadComplete={handleUploadComplete}
                onMappingChange={setMappingState}
            />
        },
        {
            name: 'summary',
            label: summaryStepLabel!,
            description: summaryStepDescription,
            nextStepLabel: importButtonLabel,
            content: <ImportSummaryStep file={selectedFile} mappingState={mappingState} />
        }
    ], [
        columnHeaderLabel, contentLabel, dataNameLabel, dataTypeLabel, dateFormatLabel,
        downloadCSVSampleLabel, downloadExcelSampleLabel, effectiveRequiredColumns, entity,
        formAPI.status.loading, handleDeleteFile, handleUploadComplete, handleValidateFile,
        importButtonLabel, importDestinations, importEntity, instructionsLabel,
        instructionsStepDescription, instructionsStepLabel, mappingState, numberFormatLabel,
        selectedFile, summaryStepDescription, summaryStepLabel, uploadStepDescription,
        uploadStepLabel, uploadValidationError, validateUploadStep
    ]);

    const completedContent = importData.importStatus.data && selectedFile
        ? <ImportCompletedContent
            fileName={selectedFile.name}
            result={importData.importStatus.data}
            onClose={onCancel}
            importedFileLabel={importedFileLabel}
            recordsImportedSuccessfullyLabel={recordsImportedSuccessfullyLabel}
            recordsNotImportedDueToErrorsLabel={recordsNotImportedDueToErrorsLabel}
            errorColumnTitle={errorColumnTitle}
            rowColumnTitle={rowColumnTitle}
            closeLabel={CloseText || 'Close'}
        />
        : undefined;

    return (
        <StepperForm
            formAPI={formAPI}
            steps={steps}
            initialStep={0}
            completedContent={completedContent}
            onCancelSubmit={importData.cancel}
            cancelSubmitLabel={CancelText || 'Cancel'}
            nextStepLabel="Next step"
            prevStepLabel="Back"
            showErrors
        />
    );
}
