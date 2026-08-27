import { ActionIcon, Alert, Button, Card, Group, NumberInput, ScrollArea, Select, Stack, Table, Text, TextInput, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconAlertCircle, IconPlus, IconTrash } from "@tabler/icons-react";
import ExcelJS from "exceljs";
import { useCallback, useEffect, useMemo, useState } from "react";
import { ExcelImportMapping, ImportDataMapping } from "../../client";

interface EditorMappingRow {
    SourceHeader: string,
    SourceIndex: number | null,
    DestinationColumnName: string | null,
}

export interface ExcelImportMappingEditorProps {
    file: File,
    destinations: readonly string[],
    onChange: (mapping: ExcelImportMapping | undefined, initialRow: number) => void,
    initialRow?: number,
    sampleRowCount?: number,
    worksheetLabel?: string,
    initialRowLabel?: string,
    sourceHeaderLabel?: string,
    sourceIndexLabel?: string,
    destinationLabel?: string,
    previewLabel?: string,
    addMappingLabel?: string,
    legacyHelpLabel?: string,
    noDestinationsLabel?: string,
    workbookErrorLabel?: string,
}

export const ExcelImportMappingEditorDefaultProps: Partial<ExcelImportMappingEditorProps> = {
    initialRow: 1,
    sampleRowCount: 10,
    worksheetLabel: "Worksheet",
    initialRowLabel: "Header row",
    sourceHeaderLabel: "Source header",
    sourceIndexLabel: "Source column",
    destinationLabel: "Destination",
    previewLabel: "Sample rows",
    addMappingLabel: "Add mapping",
    legacyHelpLabel: "Legacy .xls files cannot be previewed locally. Save the file as .xlsx to enable worksheet selection and preview.",
    noDestinationsLabel: "No import destinations are available.",
    workbookErrorLabel: "The Excel workbook could not be read.",
}

function getFileExtension(file: File) {
    const separator = file.name.lastIndexOf('.');
    return separator >= 0 ? file.name.slice(separator).toLowerCase() : '';
}

function emptyManualMapping(): EditorMappingRow {
    return { SourceHeader: '', SourceIndex: null, DestinationColumnName: null };
}

function toContractMapping(rows: EditorMappingRow[]) {
    return rows
        .filter(row => row.DestinationColumnName && (row.SourceHeader.trim() || row.SourceIndex !== null))
        .map<ImportDataMapping>(row => ({
            SourceHeader: row.SourceHeader.trim() ? row.SourceHeader : null,
            SourceIndex: row.SourceIndex,
            DestinationColumnName: row.DestinationColumnName!
        }));
}

function hasDuplicateDestinations(rows: EditorMappingRow[]) {
    const destinations = rows
        .map(row => row.DestinationColumnName?.toLowerCase())
        .filter((destination): destination is string => !!destination);
    return new Set(destinations).size !== destinations.length;
}

function readWorksheet(worksheet: ExcelJS.Worksheet, initialRow: number, destinations: readonly string[], sampleRowCount: number) {
    const excelHeaderRow = worksheet.getRow(initialRow);
    const columnCount = excelHeaderRow.cellCount;
    const destinationNames = new Map(destinations.map(destination => [destination.toLowerCase(), destination]));
    const usedDestinations = new Set<string>();
    const rows: EditorMappingRow[] = [];

    for (let columnIndex = 1; columnIndex <= columnCount; columnIndex++) {
        const sourceHeader = excelHeaderRow.getCell(columnIndex).text;
        const matchingDestination = destinationNames.get(sourceHeader.trim().toLowerCase());
        const canUseDestination = matchingDestination && !usedDestinations.has(matchingDestination.toLowerCase());

        if (canUseDestination) usedDestinations.add(matchingDestination.toLowerCase());
        rows.push({
            SourceHeader: sourceHeader,
            SourceIndex: columnIndex - 1,
            DestinationColumnName: canUseDestination ? matchingDestination : null
        });
    }

    const samples: string[][] = [];
    for (let rowIndex = initialRow + 1; rowIndex <= worksheet.rowCount && samples.length < sampleRowCount; rowIndex++) {
        const worksheetRow = worksheet.getRow(rowIndex);
        const values = Array.from({ length: columnCount }, (_, index) => worksheetRow.getCell(index + 1).text);
        if (values.some(value => value.trim() !== '')) samples.push(values);
    }

    return { rows, samples };
}

export function ExcelImportMappingEditor(props: ExcelImportMappingEditorProps) {
    const {
        file, destinations, onChange, initialRow, sampleRowCount, worksheetLabel, initialRowLabel,
        sourceHeaderLabel, sourceIndexLabel, destinationLabel, previewLabel, addMappingLabel,
        legacyHelpLabel, noDestinationsLabel, workbookErrorLabel
    } = useComponentDefaultProps('ExcelImportMappingEditor', ExcelImportMappingEditorDefaultProps, props);

    const theme = useMantineTheme();
    const extension = getFileExtension(file);
    const isLegacyWorkbook = extension === '.xls';
    const [workbook, setWorkbook] = useState<ExcelJS.Workbook | null>(null);
    const [worksheetNames, setWorksheetNames] = useState<string[]>([]);
    const [sheetName, setSheetName] = useState('');
    const [headerRow, setHeaderRow] = useState(initialRow!);
    const [mappingRows, setMappingRows] = useState<EditorMappingRow[]>(isLegacyWorkbook ? [emptyManualMapping()] : []);
    const [sampleRows, setSampleRows] = useState<string[][]>([]);
    const [loading, setLoading] = useState(!isLegacyWorkbook);
    const [error, setError] = useState<string>();

    const destinationData = useMemo(() => destinations.map(destination => ({ value: destination, label: destination })), [destinations]);

    const emitChange = useCallback((rows: EditorMappingRow[], currentSheetName: string, currentInitialRow: number) => {
        const mapping = toContractMapping(rows);
        if (!mapping.length || hasDuplicateDestinations(rows)) {
            onChange(undefined, currentInitialRow);
            return;
        }
        onChange({ SheetName: currentSheetName.trim() || null, Mapping: mapping }, currentInitialRow);
    }, [onChange]);

    const applyWorksheet = useCallback((currentWorkbook: ExcelJS.Workbook, currentSheetName: string, currentInitialRow: number) => {
        const worksheet = currentWorkbook.getWorksheet(currentSheetName);
        if (!worksheet) {
            setMappingRows([]);
            setSampleRows([]);
            onChange(undefined, currentInitialRow);
            return;
        }

        const result = readWorksheet(worksheet, currentInitialRow, destinations, sampleRowCount!);
        setMappingRows(result.rows);
        setSampleRows(result.samples);
        emitChange(result.rows, currentSheetName, currentInitialRow);
    }, [destinations, emitChange, onChange, sampleRowCount]);

    useEffect(() => {
        let active = true;

        void Promise.resolve().then(async () => {
            if (!active) return;
            setHeaderRow(initialRow!);
            setError(undefined);

            if (isLegacyWorkbook) {
                const rows = [emptyManualMapping()];
                setWorkbook(null);
                setWorksheetNames([]);
                setSheetName('');
                setMappingRows(rows);
                setSampleRows([]);
                setLoading(false);
                onChange(undefined, initialRow!);
                return;
            }

            setLoading(true);
            try {
                const nextWorkbook = new ExcelJS.Workbook();
                await nextWorkbook.xlsx.load(await file.arrayBuffer());
                if (!active) return;

                const names = nextWorkbook.worksheets.map(worksheet => worksheet.name);
                const firstWorksheetName = names[0] ?? '';
                setWorkbook(nextWorkbook);
                setWorksheetNames(names);
                setSheetName(firstWorksheetName);
                setLoading(false);

                if (!firstWorksheetName) {
                    setMappingRows([]);
                    setSampleRows([]);
                    onChange(undefined, initialRow!);
                    return;
                }
                applyWorksheet(nextWorkbook, firstWorksheetName, initialRow!);
            }
            catch (reason: unknown) {
                if (!active) return;
                setWorkbook(null);
                setWorksheetNames([]);
                setMappingRows([]);
                setSampleRows([]);
                setLoading(false);
                setError(reason instanceof Error ? `${workbookErrorLabel} ${reason.message}` : workbookErrorLabel);
                onChange(undefined, initialRow!);
            }
        });

        return () => { active = false; };
    }, [applyWorksheet, file, initialRow, isLegacyWorkbook, onChange, workbookErrorLabel]);

    const updateMappingRow = (rowIndex: number, update: Partial<EditorMappingRow>) => {
        const rows = mappingRows.map((row, index) => index === rowIndex ? { ...row, ...update } : row);
        setMappingRows(rows);
        emitChange(rows, sheetName, headerRow);
    };

    const removeMappingRow = (rowIndex: number) => {
        const rows = mappingRows.filter((_, index) => index !== rowIndex);
        setMappingRows(rows);
        emitChange(rows, sheetName, headerRow);
    };

    const usedDestinations = mappingRows
        .map(row => row.DestinationColumnName)
        .filter((destination): destination is string => !!destination);

    const mappingTable = (
        <ScrollArea type="auto">
            <Table striped highlightOnHover withBorder withColumnBorders miw={isLegacyWorkbook ? 720 : 520}>
                <thead>
                    <tr>
                        <th>{sourceHeaderLabel}</th>
                        <th>{sourceIndexLabel}</th>
                        <th>{destinationLabel}</th>
                        {isLegacyWorkbook && <th></th>}
                    </tr>
                </thead>
                <tbody>
                    {mappingRows.map((row, rowIndex) => (
                        <tr key={`${row.SourceIndex ?? 'manual'}-${rowIndex}`}>
                            <td>
                                {isLegacyWorkbook
                                    ? <TextInput
                                        value={row.SourceHeader}
                                        onChange={event => updateMappingRow(rowIndex, { SourceHeader: event.currentTarget.value })}
                                    />
                                    : <Text size="sm">{row.SourceHeader.trim() || `#${row.SourceIndex! + 1}`}</Text>
                                }
                            </td>
                            <td>
                                {isLegacyWorkbook
                                    ? <NumberInput
                                        value={row.SourceIndex === null ? '' : row.SourceIndex + 1}
                                        min={1}
                                        precision={0}
                                        onChange={value => updateMappingRow(rowIndex, { SourceIndex: value === '' ? null : value - 1 })}
                                    />
                                    : <Text size="sm">{row.SourceIndex! + 1}</Text>
                                }
                            </td>
                            <td>
                                <Select
                                    clearable
                                    searchable
                                    data={destinationData.map(destination => ({
                                        ...destination,
                                        disabled: destination.value !== row.DestinationColumnName
                                            && usedDestinations.some(usedDestination => usedDestination.toLowerCase() === destination.value.toLowerCase())
                                    }))}
                                    value={row.DestinationColumnName}
                                    onChange={value => updateMappingRow(rowIndex, { DestinationColumnName: value })}
                                />
                            </td>
                            {isLegacyWorkbook &&
                                <td>
                                    <ActionIcon
                                        color="red"
                                        variant="subtle"
                                        onClick={() => removeMappingRow(rowIndex)}
                                    >
                                        <IconTrash size="1rem" />
                                    </ActionIcon>
                                </td>
                            }
                        </tr>
                    ))}
                </tbody>
            </Table>
        </ScrollArea>
    );

    if (!destinations.length) {
        return <Alert color="yellow" icon={<IconAlertCircle size="1rem" />}>{noDestinationsLabel}</Alert>;
    }

    return (
        <Card withBorder bg={theme.colorScheme === 'dark' ? theme.colors.dark[6] : theme.white}>
            <Stack spacing="sm">
                {error && <Alert color="red" icon={<IconAlertCircle size="1rem" />}>{error}</Alert>}
                {isLegacyWorkbook && <Alert color="yellow" icon={<IconAlertCircle size="1rem" />}>{legacyHelpLabel}</Alert>}
                <Group grow align="end">
                    {isLegacyWorkbook
                        ? <TextInput
                            label={worksheetLabel}
                            value={sheetName}
                            onChange={event => {
                                const value = event.currentTarget.value;
                                setSheetName(value);
                                emitChange(mappingRows, value, headerRow);
                            }}
                        />
                        : <Select
                            label={worksheetLabel}
                            data={worksheetNames}
                            value={sheetName}
                            onChange={value => {
                                const nextSheetName = value ?? '';
                                setSheetName(nextSheetName);
                                if (workbook && nextSheetName) applyWorksheet(workbook, nextSheetName, headerRow);
                            }}
                        />
                    }
                    <NumberInput
                        label={initialRowLabel}
                        value={headerRow}
                        min={1}
                        precision={0}
                        onChange={value => {
                            const nextHeaderRow = value === '' ? 1 : Math.max(1, value);
                            setHeaderRow(nextHeaderRow);
                            if (isLegacyWorkbook) emitChange(mappingRows, sheetName, nextHeaderRow);
                            else if (workbook && sheetName) applyWorksheet(workbook, sheetName, nextHeaderRow);
                        }}
                    />
                </Group>
                {!loading && !error && mappingTable}
                {isLegacyWorkbook &&
                    <Button
                        variant="light"
                        leftIcon={<IconPlus size="1rem" />}
                        disabled={mappingRows.length >= destinations.length}
                        onClick={() => {
                            const rows = [...mappingRows, emptyManualMapping()];
                            setMappingRows(rows);
                            emitChange(rows, sheetName, headerRow);
                        }}
                    >
                        {addMappingLabel}
                    </Button>
                }
                {!isLegacyWorkbook && !loading && !error && sampleRows.length > 0 &&
                    <Stack spacing="xs">
                        <Text size="sm" fw={500}>{previewLabel}</Text>
                        <ScrollArea type="auto">
                            <Table withBorder withColumnBorders miw={Math.max(520, mappingRows.length * 120)}>
                                <thead>
                                    <tr>{mappingRows.map((row, index) => <th key={index}>{row.SourceHeader.trim() || `#${index + 1}`}</th>)}</tr>
                                </thead>
                                <tbody>
                                    {sampleRows.map((row, rowIndex) => (
                                        <tr key={rowIndex}>{row.map((value, columnIndex) => <td key={columnIndex}><Text size="xs">{value}</Text></td>)}</tr>
                                    ))}
                                </tbody>
                            </Table>
                        </ScrollArea>
                    </Stack>
                }
            </Stack>
        </Card>
    );
}
