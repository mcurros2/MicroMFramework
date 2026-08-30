import { useComponentDefaultProps } from "@mantine/core";
import { useCallback, useMemo, useRef, useState } from "react";
import { DBStatusResult, OperationStatus } from "../../client";
import { Entity, EntityDefinition, getRequiredColumns } from "../../Entity";
import { FormOptions } from "../../UI/Core/types";
import { UploadCompletionResult, UploadProgressReport, ValidateFileReturnType } from "../../UI/FileUploader";
import { StepperForm, StepperFormStep, useEntityForm } from "../../UI/Form";
import { useImportData, useImportDataMapping } from "../../UI/ImportData";
import { ImportCompletedContent } from "./ImportCompletedContent";
import { ImportDataMappingStep } from "./ImportDataMappingStep";
import { ImportEntityData } from "./ImportEntityData";
import { ImportFileStep } from "./ImportFileStep";
import { ImportInstructionsStep } from "./ImportInstructionsStep";
import { ImportSampleDataStep } from "./ImportSampleDataStep";
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
    dataMappingStepLabel?: string,
    dataMappingStepDescription?: string,
    sampleDataStepLabel?: string,
    sampleDataStepDescription?: string,
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
    confirmDataMappingLabel?: string,
    importButtonLabel?: string,
    submitAndImportLabel?: string,
    importingDataLabel?: string,
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
    uploadStepDescription: "Upload the import file",
    dataMappingStepLabel: "Data mapping",
    dataMappingStepDescription: "Review and edit mapped columns",
    sampleDataStepLabel: "View sample data",
    sampleDataStepDescription: "Confirm data mapping",
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
    confirmDataMappingLabel: "Confirm that the data mapping matches the sample data before continuing.",
    importButtonLabel: "Import data",
    submitAndImportLabel: "Submit and import",
    importingDataLabel: "Importing data",
    importedFileLabel: "Imported file",
    recordsImportedSuccessfullyLabel: "Records imported successfully",
    recordsNotImportedDueToErrorsLabel: "Records not imported due to errors",
    errorColumnTitle: "Error",
    rowColumnTitle: "Row",
};

export function ImportEntityDataForm(props: ImportEntityDataFormProps) {
    const {
        entity, initialFormMode, getDataOnInit, onCancel, importEntity, entityProcName,
        excludedImportDestinations, onImportSuccess, instructionsStepLabel, instructionsStepDescription,
        uploadStepLabel, uploadStepDescription, dataMappingStepLabel, dataMappingStepDescription,
        sampleDataStepLabel, sampleDataStepDescription, summaryStepLabel, summaryStepDescription,
        instructionsLabel, columnHeaderLabel, dataNameLabel, contentLabel, dataTypeLabel,
        dateFormatLabel, numberFormatLabel, downloadExcelSampleLabel, downloadCSVSampleLabel,
        errorReadingFileLabel, emptyFileLabel, uploadRequiredLabel, mappingRequiredLabel,
        confirmDataMappingLabel, importButtonLabel, submitAndImportLabel, importingDataLabel,
        importedFileLabel, recordsImportedSuccessfullyLabel,
        recordsNotImportedDueToErrorsLabel, errorColumnTitle, rowColumnTitle, CancelText, CloseText
    } = useComponentDefaultProps('ImportEntityDataForm', ImportEntityDataFormDefaultProps, props);

    const importData = useImportData(importEntity);
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [uploadCompleted, setUploadCompleted] = useState(false);
    const [uploadValidationError, setUploadValidationError] = useState<string>();
    const [mappingValidationError, setMappingValidationError] = useState<string>();
    const [reviewedMappingRevision, setReviewedMappingRevision] = useState<number>();
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

    const mappingAPI = useImportDataMapping({
        destinations: importDestinations,
        requiredDestinations: effectiveRequiredColumns,
        fileErrorLabel: errorReadingFileLabel
    });
    const mappingState = mappingAPI.mappingState;
    const sampleReviewed = mappingState?.isValid && reviewedMappingRevision === mappingAPI.revision;

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
        setUploadValidationError(undefined);
        setMappingValidationError(undefined);
        setReviewedMappingRevision(undefined);
        importSuccessNotified.current = false;

        void mappingAPI.loadFile(file);

        return { error: false };
    }, [emptyFileLabel, errorReadingFileLabel, mappingAPI]);

    const handleDeleteFile = useCallback(() => {
        setSelectedFile(null);
        setUploadCompleted(false);
        setUploadValidationError(undefined);
        setMappingValidationError(undefined);
        setReviewedMappingRevision(undefined);
        importSuccessNotified.current = false;
        mappingAPI.reset();

        return true;
    }, [mappingAPI]);

    const handleUploadComplete = useCallback(async (_report: UploadProgressReport): Promise<UploadCompletionResult> => {
        setUploadCompleted(true);
        return { error: false };
    }, []);

    const validateUploadStep = useCallback(() => {
        if (!selectedFile || !uploadCompleted || !entity.def.columns.c_fileprocess_id.value) {
            setUploadValidationError(uploadRequiredLabel);
            return false;
        }

        setUploadValidationError(undefined);
        return true;
    }, [entity.def.columns.c_fileprocess_id.value, selectedFile, uploadCompleted, uploadRequiredLabel]);

    const validateMappingStep = useCallback(() => {
        if (!mappingState?.isValid) {
            setMappingValidationError(mappingRequiredLabel);
            return false;
        }

        setMappingValidationError(undefined);
        return true;
    }, [mappingRequiredLabel, mappingState?.isValid]);

    const validateSampleDataStep = useCallback(() => {
        if (!mappingState?.isValid) return false;
        setReviewedMappingRevision(mappingAPI.revision);
        return true;
    }, [mappingAPI.revision, mappingState?.isValid]);

    const mappingStepUnlocked = !!selectedFile && uploadCompleted && !!entity.def.columns.c_fileprocess_id.value;

    const steps = useMemo<StepperFormStep[]>(() => [
        {
            name: 'instructions',
            label: instructionsStepLabel!,
            description: instructionsStepDescription,
            allowStepSelect: true,
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
            allowStepSelect: true,
            nextStepValidation: validateUploadStep,
            content: <ImportFileStep
                entity={entity}
                disabled={formAPI.status.loading}
                validationError={mappingStepUnlocked ? undefined : uploadValidationError}
                onValidateFile={handleValidateFile}
                onDelete={handleDeleteFile}
                onUploadComplete={handleUploadComplete}
            />
        },
        {
            name: 'mapping',
            label: dataMappingStepLabel!,
            description: dataMappingStepDescription,
            allowStepSelect: mappingStepUnlocked,
            nextStepValidation: validateMappingStep,
            content: <ImportDataMappingStep
                mappingAPI={mappingAPI}
                destinations={importDestinations}
                validationError={mappingState?.isValid ? undefined : mappingValidationError}
            />
        },
        {
            name: 'sample',
            label: sampleDataStepLabel!,
            description: sampleDataStepDescription,
            allowStepSelect: !!mappingState?.isValid,
            nextStepValidation: validateSampleDataStep,
            content: <ImportSampleDataStep
                mappingAPI={mappingAPI}
                confirmationLabel={confirmDataMappingLabel}
            />
        },
        {
            name: 'summary',
            label: summaryStepLabel!,
            description: summaryStepDescription,
            allowStepSelect: !!sampleReviewed,
            nextStepLabel: importButtonLabel,
            content: <ImportSummaryStep
                file={selectedFile}
                mappingState={mappingState}
                importStatus={importData.importStatus}
                submitAndImportLabel={submitAndImportLabel}
                importingDataLabel={importingDataLabel}
            />
        }
    ], [
        columnHeaderLabel, confirmDataMappingLabel, contentLabel, dataMappingStepDescription,
        dataMappingStepLabel, dataNameLabel, dataTypeLabel, dateFormatLabel,
        downloadCSVSampleLabel, downloadExcelSampleLabel, effectiveRequiredColumns, entity,
        formAPI.status.loading, handleDeleteFile, handleUploadComplete, handleValidateFile,
        importButtonLabel, importData.importStatus, importDestinations, importEntity, importingDataLabel,
        instructionsLabel, instructionsStepDescription, instructionsStepLabel, mappingAPI,
        mappingState, mappingStepUnlocked, mappingValidationError, numberFormatLabel,
        sampleDataStepDescription, sampleDataStepLabel, sampleReviewed, selectedFile,
        submitAndImportLabel, summaryStepDescription, summaryStepLabel, uploadStepDescription,
        uploadStepLabel, uploadValidationError, validateMappingStep, validateSampleDataStep,
        validateUploadStep
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
