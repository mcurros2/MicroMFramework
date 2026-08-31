import { useComponentDefaultProps } from "@mantine/core";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { DBStatusResult, OperationStatus } from "../../client";
import { Entity, EntityColumnFlags, EntityDefinition, getRequiredColumns } from "../../Entity";
import { FormOptions } from "../../UI/Core/types";
import { UploadCompletionResult, UploadProgressReport, ValidateFileReturnType } from "../../UI/FileUploader";
import { StepperForm, StepperFormStep, useEntityForm } from "../../UI/Form";
import { useImportData, useImportDataMapping } from "../../UI/ImportData";
import { ImportCompletedContent } from "./ImportCompletedContent";
import { ImportDataMappingStep } from "./ImportDataMappingStep";
import { ImportEntityData } from "./ImportEntityData";
import { ImportFileStep, ImportFileStepCustomizationProps } from "./ImportFileStep";
import { ImportInstructionsStep } from "./ImportInstructionsStep";
import { ImportSummaryStep } from "./ImportSummaryStep";

export interface ImportEntityDataFormProps extends FormOptions<ImportEntityData> {
    importEntity?: Entity<EntityDefinition>,
    destinationEntityExportViewName?: string,
    entityProcName?: string,
    excludedImportDestinations?: string[],
    importFileStepProps?: ImportFileStepCustomizationProps,
    onImportSuccess?: () => Promise<void>,
    instructionsStepLabel?: string,
    instructionsStepDescription?: string,
    uploadStepLabel?: string,
    uploadStepDescription?: string,
    dataMappingStepLabel?: string,
    dataMappingStepDescription?: string,
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
    exportExistingDataLabel?: string,
    exportExistingDataErrorLabel?: string,
    errorReadingFileLabel?: string,
    emptyFileLabel?: string,
    uploadRequiredLabel?: string,
    mappingRequiredLabel?: string,
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
    mappingRequiredLabel: "Complete the column mapping before importing.",
    importButtonLabel: "Import data",
    submitAndImportLabel: "Ready to submit and import",
    importingDataLabel: "Importing data",
    importedFileLabel: "Imported file",
    recordsImportedSuccessfullyLabel: "Records imported successfully",
    recordsNotImportedDueToErrorsLabel: "Records not imported due to errors",
    errorColumnTitle: "Error",
    rowColumnTitle: "Row",
};

export function ImportEntityDataForm(props: ImportEntityDataFormProps) {
    const {
        entity, initialFormMode, getDataOnInit, onCancel, importEntity, destinationEntityExportViewName, entityProcName,
        excludedImportDestinations, importFileStepProps, onImportSuccess, instructionsStepLabel, instructionsStepDescription,
        uploadStepLabel, uploadStepDescription, dataMappingStepLabel, dataMappingStepDescription,
        summaryStepLabel, summaryStepDescription,
        instructionsLabel, columnHeaderLabel, dataNameLabel, contentLabel, dataTypeLabel,
        dateFormatLabel, numberFormatLabel, downloadExcelSampleLabel, downloadCSVSampleLabel,
        exportExistingDataLabel, exportExistingDataErrorLabel,
        errorReadingFileLabel, emptyFileLabel, uploadRequiredLabel, mappingRequiredLabel,
        importButtonLabel, submitAndImportLabel, importingDataLabel,
        importedFileLabel, recordsImportedSuccessfullyLabel,
        recordsNotImportedDueToErrorsLabel, errorColumnTitle, rowColumnTitle, CancelText, CloseText
    } = useComponentDefaultProps('ImportEntityDataForm', ImportEntityDataFormDefaultProps, props);

    const importData = useImportData(importEntity);
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [uploadCompleted, setUploadCompleted] = useState(false);
    const [uploadValidationError, setUploadValidationError] = useState<string>();
    const [mappingValidationError, setMappingValidationError] = useState<string>();
    const importSuccessNotified = useRef(false);

    const requiredColumns = useMemo(
        () => importEntity?.def.importColumns || getRequiredColumns(importEntity),
        [importEntity]
    );

    const requestedDestinationEntityExportViewName = destinationEntityExportViewName || importEntity?.def.standardView();
    const resolvedDestinationEntityExportViewName = requestedDestinationEntityExportViewName &&
        importEntity?.def.views[requestedDestinationEntityExportViewName]
        ? requestedDestinationEntityExportViewName
        : undefined;

    useEffect(() => {
        if (destinationEntityExportViewName && importEntity && !importEntity.def.views[destinationEntityExportViewName]) {
            console.warn(
                `Export existing data: view '${destinationEntityExportViewName}' was not found in entity '${importEntity.name}'.`
            );
        }
    }, [destinationEntityExportViewName, importEntity]);

    const primaryKeyColumns = useMemo(
        () => Object.values(importEntity?.def.columns ?? {})
            .filter(column => column.hasFlag(EntityColumnFlags.pk)),
        [importEntity]
    );

    const importDestinations = useMemo(() => {
        const entityProc = entityProcName ? importEntity?.def.procs[entityProcName] : undefined;
        const destinations = entityProcName
            ? Object.keys(entityProc?.parms ?? {})
            : Array.from(new Set([...requiredColumns, ...primaryKeyColumns.map(column => column.name)]));
        const excluded = new Set((excludedImportDestinations ?? []).map(destination => destination.toLowerCase()));

        return destinations.filter(destination => !excluded.has(destination.toLowerCase()));
    }, [entityProcName, excludedImportDestinations, importEntity, primaryKeyColumns, requiredColumns]);

    const effectiveRequiredColumns = useMemo(() => {
        const allowed = new Set(importDestinations.map(destination => destination.toLowerCase()));

        return requiredColumns.filter(column => allowed.has(column.toLowerCase()));
    }, [importDestinations, requiredColumns]);

    const primaryKeyMapping = useMemo(() => {
        const allowed = new Set(importDestinations.map(destination => destination.toLowerCase()));
        const availablePrimaryKeyColumns = primaryKeyColumns
            .filter(column => allowed.has(column.name.toLowerCase()));

        return {
            requiredDestinations: availablePrimaryKeyColumns.map(column => column.name),
            omittableRequiredDestinations: availablePrimaryKeyColumns
                .filter(column => column.hasFlag(EntityColumnFlags.autoNum))
                .map(column => column.name)
        };
    }, [importDestinations, primaryKeyColumns]);

    const mappingAPI = useImportDataMapping({
        destinations: importDestinations,
        requiredDestinations: primaryKeyMapping.requiredDestinations,
        omittableRequiredDestinations: primaryKeyMapping.omittableRequiredDestinations,
        fileErrorLabel: errorReadingFileLabel
    });
    const mappingState = mappingAPI.mappingState;

    const executeImport = useCallback(async (): Promise<OperationStatus<DBStatusResult>> => {
        const fileProcessID = entity.def.columns.c_fileprocess_id.value;

        if (!fileProcessID || !mappingState?.isSuccessful) {
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
        importSuccessNotified.current = false;

        void mappingAPI.loadFile(file);

        return { error: false };
    }, [emptyFileLabel, errorReadingFileLabel, mappingAPI]);

    const handleDeleteFile = useCallback(() => {
        setSelectedFile(null);
        setUploadCompleted(false);
        setUploadValidationError(undefined);
        setMappingValidationError(undefined);
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
        if (!mappingState) {
            setMappingValidationError(mappingRequiredLabel);
            return false;
        }

        setMappingValidationError(undefined);
        return true;
    }, [mappingRequiredLabel, mappingState]);

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
                exportViewName={resolvedDestinationEntityExportViewName}
                instructionsLabel={instructionsLabel}
                columnHeaderLabel={columnHeaderLabel}
                dataNameLabel={dataNameLabel}
                contentLabel={contentLabel}
                dataTypeLabel={dataTypeLabel}
                dateFormatLabel={dateFormatLabel}
                numberFormatLabel={numberFormatLabel}
                downloadExcelSampleLabel={downloadExcelSampleLabel}
                downloadCSVSampleLabel={downloadCSVSampleLabel}
                exportExistingDataLabel={exportExistingDataLabel}
                exportExistingDataErrorLabel={exportExistingDataErrorLabel}
            />
        },
        {
            name: 'upload',
            label: uploadStepLabel!,
            description: uploadStepDescription,
            allowStepSelect: true,
            nextStepValidation: validateUploadStep,
            content: <ImportFileStep
                {...importFileStepProps}
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
                validationError={mappingState ? undefined : mappingValidationError}
            />
        },
        {
            name: 'summary',
            label: summaryStepLabel!,
            description: summaryStepDescription,
            allowStepSelect: !!mappingState,
            nextStepLabel: importButtonLabel,
            submitDisabled: !mappingState?.isSuccessful,
            content: <ImportSummaryStep
                file={selectedFile}
                mappingAPI={mappingAPI}
                importStatus={importData.importStatus}
                submitAndImportLabel={submitAndImportLabel}
                importingDataLabel={importingDataLabel}
            />
        }
    ], [
        columnHeaderLabel, contentLabel, dataMappingStepDescription,
        dataMappingStepLabel, dataNameLabel, dataTypeLabel, dateFormatLabel,
        downloadCSVSampleLabel, downloadExcelSampleLabel, effectiveRequiredColumns, entity,
        exportExistingDataErrorLabel, exportExistingDataLabel,
        formAPI.status.loading, handleDeleteFile, handleUploadComplete, handleValidateFile,
        importButtonLabel, importData.importStatus, importDestinations, importEntity, importFileStepProps, importingDataLabel,
        instructionsLabel, instructionsStepDescription, instructionsStepLabel, mappingAPI,
        mappingState, mappingStepUnlocked, mappingValidationError, numberFormatLabel,
        resolvedDestinationEntityExportViewName, selectedFile,
        submitAndImportLabel, summaryStepDescription, summaryStepLabel, uploadStepDescription,
        uploadStepLabel, uploadValidationError, validateMappingStep, validateUploadStep
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
