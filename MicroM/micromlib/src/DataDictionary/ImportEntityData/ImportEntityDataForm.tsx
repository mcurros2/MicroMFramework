import { Accordion, Button, Card, Group, List, Stack, Table, Text, Title, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconCheck, IconCircleCheck, IconCircleNumber1, IconCircleNumber2, IconCircleX, IconX } from "@tabler/icons-react";
import { useCallback, useMemo, useRef, useState } from "react";
import { ExcelImportMapping, Value } from "../../client";
import { Entity, EntityColumn, EntityDefinition, getRequiredColumns } from "../../Entity";
import { AlertError } from "../../UI/Core/AlertError";
import { CircleFilledIcon } from "../../UI/Core/CircleFilledIcon";
import { FakeProgressBar } from "../../UI/Core/FakeProgressBar";
import { FormOptions } from "../../UI/Core/types";
import { FilesUploadForm, FileUploaderDefaultProps } from "../../UI/FileUploader";
import { EntityForm, EntityFormDefaultProps, useEntityForm } from "../../UI/Form";
import { ExcelImportMappingEditor, useEntityCSVImportValidation, useImportData } from "../../UI/ImportData";
import { ImportEntityData } from "./ImportEntityData";

export interface ImportEntityDataFormProps extends FormOptions<ImportEntityData> {
    importEntity?: Entity<EntityDefinition>,
    entityProcName?: string,
    excludedImportDestinations?: string[],
    onImportSuccess?: () => Promise<void>,
    importInfoLabel?: string,
    csvInstructionsLabel?: string,
    columnHeaderLabel?: string,
    dataNameLabel?: string,
    contentLabel?: string,
    dataTypeLabel?: string,
    errorReadingFileLabel?: string,
    emptyCSVFileLabel?: string,
    missingColumnsLabel?: string,
    dateFormatLabel?: string,
    numberFormatLabel?: string,
    downloadSampleLabel?: string,
    importButtonLabel?: string,
    importHelpAndInstructionsLabel?: string,
    recordsImportedSusccessfullyLabel?: string,
    recordsNotImportedDueToErrorsLabel?: string,
    importTheCSVFileLabel?: string,
    importedFileLabel?: string,
    errorColumnTitle?: string,
    rowColumnTitle?: string,
}

export const ImportEntityDataFormDefaultProps: Partial<ImportEntityDataFormProps> = {
    initialFormMode: "view",
    importInfoLabel: "Import a CSV or Excel file with data for",
    csvInstructionsLabel: "CSV files must contain a header with these column names. Excel columns can be mapped after selecting a file:",
    columnHeaderLabel: "Column Header",
    dataNameLabel: "Data Name",
    contentLabel: "Content",
    dataTypeLabel: "Data Type",
    errorReadingFileLabel: "Error reading file",
    emptyCSVFileLabel: "The CSV file is empty",
    missingColumnsLabel: "Missing columns:",
    dateFormatLabel: "* Dates should have the format YYYY-MM-DD",
    numberFormatLabel: "* Numbers with decimal places need to use '.' as separator",
    downloadSampleLabel: "Download a sample CSV",
    importButtonLabel: "Import",
    importHelpAndInstructionsLabel: "Help and Instructions",
    recordsImportedSusccessfullyLabel: "Records imported successfully",
    recordsNotImportedDueToErrorsLabel: "Records not imported due to errors",
    importTheCSVFileLabel: "Select a CSV or Excel file by clicking or dragging it, then click",
    importedFileLabel: "Imported file",
    errorColumnTitle: "Error",
    rowColumnTitle: "Row",
}

export function ImportEntityDataForm(props: ImportEntityDataFormProps) {

    const {
        entity, initialFormMode, getDataOnInit, onSaved, onCancel, csvInstructionsLabel,
        columnHeaderLabel, dataNameLabel, contentLabel, dataTypeLabel,
        errorReadingFileLabel, emptyCSVFileLabel, missingColumnsLabel, importEntity,
        dateFormatLabel, numberFormatLabel, downloadSampleLabel, importButtonLabel, CancelText, importHelpAndInstructionsLabel,
        recordsImportedSusccessfullyLabel, recordsNotImportedDueToErrorsLabel, CloseText, importTheCSVFileLabel,
        importedFileLabel, errorColumnTitle, rowColumnTitle, entityProcName, excludedImportDestinations, onImportSuccess
    } = useComponentDefaultProps('ImportEntityDataForm', ImportEntityDataFormDefaultProps, props);

    const theme = useMantineTheme();

    const formAPI = useEntityForm({ entity: entity, initialFormMode, getDataOnInit: getDataOnInit!, onSaved, onCancel, noSaveOnSubmit: true });

    const validation = useEntityCSVImportValidation({ emptyCSVFileLabel, errorReadingFileLabel, missingColumnsLabel });

    const importData = useImportData(importEntity);

    const required = useMemo(() => importEntity?.def.importColumns || getRequiredColumns(importEntity), [importEntity]);

    const importDestinations = useMemo(() => {
        const entityProc = entityProcName ? importEntity?.def.procs[entityProcName] : undefined;
        const destinations = entityProcName ? Object.keys(entityProc?.parms ?? {}) : required;
        const excluded = new Set((excludedImportDestinations ?? []).map(destination => destination.toLowerCase()));

        return destinations.filter(destination => !excluded.has(destination.toLowerCase()));
    }, [entityProcName, excludedImportDestinations, importEntity, required]);

    const [lastProcessedFile, setLastProcessedFile] = useState<string | null>(null);
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [excelMapping, setExcelMapping] = useState<ExcelImportMapping>();
    const [excelInitialRow, setExcelInitialRow] = useState(1);
    const [toggleAccordion, setToggleAccordion] = useState<string | null>(null);

    const importSuccessNotified = useRef(false);

    const selectedFileExtension = selectedFile?.name.slice(selectedFile.name.lastIndexOf('.')).toLocaleLowerCase();
    const isExcelFile = selectedFileExtension === '.xlsx' || selectedFileExtension === '.xls';

    const handleValidateFile = useCallback(
        async (file: File) => {
            const extension = file.name.slice(file.name.lastIndexOf('.')).toLocaleLowerCase();
            const result = extension === '.csv'
                ? await validation.validateEntityImportCSVFile({ file, requiredColumns: required })
                : extension === '.xlsx' || extension === '.xls'
                    ? { error: false }
                    : { error: true, message: errorReadingFileLabel };

            if (!result.error) {
                setLastProcessedFile(file.name);
                setSelectedFile(file);
                setExcelMapping(undefined);
                setExcelInitialRow(1);
                importSuccessNotified.current = false;
            }
            return result;
        }
        , [errorReadingFileLabel, required, validation])

    const handleDeleteFile = useCallback(() => {
        setSelectedFile(null);
        setLastProcessedFile(null);
        setExcelMapping(undefined);
        setExcelInitialRow(1);
        importSuccessNotified.current = false;
        return true;
    }, []);

    const handleExcelMappingChange = useCallback((mapping: ExcelImportMapping | undefined, initialRow: number) => {
        setExcelMapping(mapping);
        setExcelInitialRow(initialRow);
    }, []);

    const handleImport = useCallback(async () => {
        if (!entity.def.columns.c_fileprocess_id.value) {
            setToggleAccordion('help');
            return;
        }

        const result = await importData.execute(
            entity.def.columns.c_fileprocess_id.value,
            entityProcName,
            isExcelFile ? excelMapping : undefined,
            isExcelFile ? excelInitialRow : undefined
        );

        if (result?.data && !importSuccessNotified.current) {
            importSuccessNotified.current = true;
            await onImportSuccess?.();
        }
    }, [entity.def.columns.c_fileprocess_id.value, entityProcName, excelInitialRow, excelMapping, importData, isExcelFile, onImportSuccess]);

    const generateSampleCSV = useCallback(() => {
        if (!importEntity) return;

        const headers = required.join(',');
        const csvContent = `data:text/csv;charset=utf-8,${headers}\n`;
        const encodedUri = encodeURI(csvContent);
        const link = document.createElement("a");

        link.setAttribute("href", encodedUri);
        link.setAttribute("download", `${importEntity.name}_sample.csv`);

        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

    }, [importEntity, required]);

    const RequiredColumns = useMemo(() => {
        return importEntity ? <Table striped withBorder withColumnBorders width="100%">
            <thead>
                <tr>
                    <th>{columnHeaderLabel}</th>
                    <th>{dataNameLabel}</th>
                    <th>{dataTypeLabel}</th>
                    <th>{contentLabel}</th>
                </tr>
            </thead>
            <tbody>
                {required.map(
                    (col_name) => {
                        const col = importEntity.def.columns[col_name as keyof typeof importEntity.def.columns] as EntityColumn<Value>;
                        const record = <tr key={col.name}>
                            <td>{col.name}</td>
                            <td>{col.prompt}</td>
                            <td>{col.type}</td>
                            <td>{col.description}</td>
                        </tr>;
                        return record;
                    }
                )
                }
            </tbody>
        </Table> : <></>
    }, [columnHeaderLabel, contentLabel, dataNameLabel, dataTypeLabel, importEntity, required]);

    return (
        <EntityForm formAPI={formAPI} showCancel={false} showOK={false}>
            <Stack>
                {!importData.importStatus.loading && !importData.importStatus.data &&
                    <>
                        <Accordion variant="separated" value={toggleAccordion} onChange={setToggleAccordion}>
                            <Accordion.Item value="help">
                                <Accordion.Control>
                                    <Text size="sm">{importHelpAndInstructionsLabel}</Text>
                                </Accordion.Control>
                                <Accordion.Panel>
                                    <Stack>
                                        <List spacing="xs" size="sm" styles={{ itemWrapper: { width: '100%' } }}>
                                            <List.Item icon={<IconCircleNumber1 size="1.75rem" />}>
                                                {`${importTheCSVFileLabel} ${importButtonLabel?.toUpperCase()}`}
                                            </List.Item>
                                            <List.Item icon={<IconCircleNumber2 size="1.75rem" />}>
                                                <Stack>
                                                    <Text>{csvInstructionsLabel}</Text>
                                                    {RequiredColumns}
                                                    <Stack spacing="0">
                                                        <Text size="xs" color="dimmed">{dateFormatLabel}</Text>
                                                        <Text size="xs" color="dimmed">{numberFormatLabel}</Text>
                                                    </Stack>
                                                    <Group>
                                                        <Button size="sm" variant="outline" onClick={generateSampleCSV}>{downloadSampleLabel}</Button>
                                                    </Group>
                                                </Stack>
                                            </List.Item>
                                        </List>
                                    </Stack>
                                </Accordion.Panel>
                            </Accordion.Item>
                        </Accordion>
                        <FilesUploadForm
                            fileProcessColumn={entity.def.columns.c_fileprocess_id}
                            client={entity.API.client}
                            maxFilesCount={1}
                            uploaderProps={{
                                ...FileUploaderDefaultProps,
                                disabled: importData.importStatus.loading || importData.importStatus.data !== undefined,
                                accept: [
                                    '.csv',
                                    '.xls',
                                    '.xlsx',
                                    'text/csv',
                                    'application/vnd.ms-excel',
                                    'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
                                ]
                            }}
                            onValidateFile={handleValidateFile}
                            onDelete={handleDeleteFile}
                        />
                        {selectedFile && isExcelFile &&
                            <ExcelImportMappingEditor
                                key={`${selectedFile.name}-${selectedFile.size}-${selectedFile.lastModified}`}
                                file={selectedFile}
                                destinations={importDestinations}
                                onChange={handleExcelMappingChange}
                            />
                        }
                    </>
                }
                {importData.importStatus.loading &&
                    <FakeProgressBar />
                }
                {importData.importStatus.error &&
                    <AlertError mt="md"><>Error: {importData.importStatus.error.status} {importData.importStatus.error.message} {importData.importStatus.error.statusMessage ?? ''}</></AlertError>
                }
                {importData.importStatus.data &&
                    <Card withBorder>
                        <Stack>
                            <Title order={6}>{importedFileLabel} {lastProcessedFile}</Title>
                            {importData.importStatus.data.SuccessCount > 0 &&
                                <Group spacing={0}><CircleFilledIcon icon={<IconCheck size="0.75rem" />} backColor={theme.colors.green[8]} />{importData.importStatus.data.SuccessCount} {recordsImportedSusccessfullyLabel}</Group>
                            }
                            {importData.importStatus.data.ErrorCount > 0 &&
                                <Group spacing={0}><CircleFilledIcon icon={<IconX size="0.75rem" />} backColor={theme.colors.red[8]} />{importData.importStatus.data.ErrorCount} {recordsNotImportedDueToErrorsLabel}</Group>
                            }
                            {importData.importStatus.data.ErrorCount > 0 &&
                                <Table withBorder striped width="100%">
                                    <thead>
                                        <tr>
                                            <th>{rowColumnTitle}</th>
                                            <th>{errorColumnTitle}</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {Object.entries(importData.importStatus.data.Errors)
                                            .slice(0, 10)
                                            .map(([key, value]) => (
                                                <tr key={key}>
                                                    <td>{key}</td>
                                                    <td>{value}</td>
                                                </tr>
                                            ))}
                                    </tbody>
                                </Table>
                            }
                        </Stack>
                    </Card>
                }
                <Group mt="xs" position="right">
                    {!importData.importStatus.loading && !importData.importStatus.data &&
                        <>
                            <Button
                                variant="light"
                                leftIcon={<IconCircleX size="1.5rem" />}
                                onClick={async () => { if (onCancel) await onCancel() }}
                            >
                                {CloseText || EntityFormDefaultProps.CloseText}
                            </Button>
                            <Button
                                onClick={handleImport}
                                loading={importData.importStatus.loading}
                                leftIcon={<IconCircleCheck size="1.5rem" />}
                                disabled={importData.importStatus.data !== undefined || (isExcelFile && !excelMapping)}
                            >
                                {importButtonLabel}
                            </Button>
                        </>
                    }
                    {importData.importStatus.loading &&
                        <Button
                            variant="light"
                            leftIcon={<IconCircleX size="1.5rem" />}
                            onClick={() => importData.cancellation.abort()}
                        >
                            {CancelText || EntityFormDefaultProps.CancelText}
                        </Button>
                    }
                    {importData.importStatus.data &&
                        <Button
                            variant="light"
                            leftIcon={<IconCircleX size="1.5rem" />}
                            onClick={async () => { if (onCancel) await onCancel() }}
                        >
                            {CloseText || EntityFormDefaultProps.CloseText}
                        </Button>
                    }
                </Group>

            </Stack>
        </EntityForm>
    )
}
