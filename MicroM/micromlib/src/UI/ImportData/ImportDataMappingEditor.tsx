import { Accordion, Alert, Center, Group, Loader, NumberInput, ScrollArea, Select, Stack, Table, Text, useComponentDefaultProps } from "@mantine/core";
import { IconAlertTriangle, IconCircleCheck } from "@tabler/icons-react";
import { useCallback, useEffect, useMemo, useState } from "react";
import { FileImportColumnMapping, FileImportMapping } from "../../client";
import { ParsedImportFile, parseImportFile } from "./ImportFileParser";

interface EditorMappingRow {
    SourceHeader: string,
    SourceIndex: number,
    DestinationColumnName: string | null,
}

export interface ImportDataMappingState {
    mapping: FileImportMapping,
    initialRow: number,
    isValid: boolean,
    isSuccessful: boolean,
    sourceColumnCount: number,
    mappedColumnCount: number,
    ignoredSourceColumns: string[],
    omittedRequiredDestinations: string[],
}

export interface ImportDataMappingEditorProps {
    file: File,
    destinations: readonly string[],
    requiredDestinations: readonly string[],
    onChange: (state: ImportDataMappingState | undefined) => void,
    initialRow?: number,
    sampleRowCount?: number,
    worksheetLabel?: string,
    initialRowLabel?: string,
    sourceHeaderLabel?: string,
    sourceIndexLabel?: string,
    destinationLabel?: string,
    mappingCorrectLabel?: string,
    mappingReviewLabel?: string,
    editMappedColumnsLabel?: string,
    viewSampleDataLabel?: string,
    noSampleDataLabel?: string,
    noDestinationsLabel?: string,
    fileErrorLabel?: string,
}

export const ImportDataMappingEditorDefaultProps: Partial<ImportDataMappingEditorProps> = {
    initialRow: 1,
    sampleRowCount: 10,
    worksheetLabel: "Worksheet",
    initialRowLabel: "Header row",
    sourceHeaderLabel: "Source header",
    sourceIndexLabel: "Source column",
    destinationLabel: "Destination",
    mappingCorrectLabel: "The column mapping is correct.",
    mappingReviewLabel: "The column mapping should be reviewed.",
    editMappedColumnsLabel: "Edit mapped columns",
    viewSampleDataLabel: "View sample data",
    noSampleDataLabel: "The file does not contain sample data below the selected header row.",
    noDestinationsLabel: "No import destinations are available.",
    fileErrorLabel: "The selected file could not be read.",
}

function sourceColumnLabel(row: EditorMappingRow) {
    return row.SourceHeader.trim() || `#${row.SourceIndex + 1}`;
}

function buildMappingRows(headers: readonly string[], destinations: readonly string[]) {
    const destinationNames = new Map(destinations.map(destination => [destination.trim().toLowerCase(), destination]));
    const usedDestinations = new Set<string>();

    return headers.map<EditorMappingRow>((sourceHeader, sourceIndex) => {
        const matchingDestination = destinationNames.get(sourceHeader.trim().toLowerCase());
        const normalizedDestination = matchingDestination?.toLowerCase();
        const canUseDestination = matchingDestination && normalizedDestination && !usedDestinations.has(normalizedDestination);
        if (canUseDestination) usedDestinations.add(normalizedDestination);

        return {
            SourceHeader: sourceHeader,
            SourceIndex: sourceIndex,
            DestinationColumnName: canUseDestination ? matchingDestination : null
        };
    });
}

function getSheet(parsedFile: ParsedImportFile, sheetName: string) {
    return parsedFile.format === 'csv'
        ? parsedFile.sheets[0]
        : parsedFile.sheets.find(sheet => sheet.name === sheetName);
}

export function ImportDataMappingEditor(props: ImportDataMappingEditorProps) {
    const {
        file, destinations, requiredDestinations, onChange, initialRow, sampleRowCount,
        worksheetLabel, initialRowLabel, sourceHeaderLabel, sourceIndexLabel, destinationLabel,
        mappingCorrectLabel, mappingReviewLabel, editMappedColumnsLabel, viewSampleDataLabel,
        noSampleDataLabel, noDestinationsLabel, fileErrorLabel
    } = useComponentDefaultProps('ImportDataMappingEditor', ImportDataMappingEditorDefaultProps, props);

    const [parsedFile, setParsedFile] = useState<ParsedImportFile>();
    const [sheetName, setSheetName] = useState('');
    const [headerRow, setHeaderRow] = useState(initialRow!);
    const [mappingRows, setMappingRows] = useState<EditorMappingRow[]>([]);
    const [sampleRows, setSampleRows] = useState<string[][]>([]);
    const [mappingState, setMappingState] = useState<ImportDataMappingState>();
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string>();

    const destinationData = useMemo(
        () => destinations.map(destination => ({ value: destination, label: destination })),
        [destinations]
    );

    const emitChange = useCallback((rows: EditorMappingRow[], currentParsedFile: ParsedImportFile, currentSheetName: string, currentHeaderRow: number) => {
        const contractRows = rows
            .filter(row => !!row.DestinationColumnName)
            .map<FileImportColumnMapping>(row => ({
                SourceHeader: row.SourceHeader.trim() || null,
                SourceIndex: row.SourceIndex,
                DestinationColumnName: row.DestinationColumnName!
            }));

        const normalizedDestinations = contractRows.map(row => row.DestinationColumnName.toLowerCase());
        const uniqueDestinations = new Set(normalizedDestinations);
        const isValid = contractRows.length > 0 && uniqueDestinations.size === normalizedDestinations.length;
        const allSourceColumnsMapped = rows.length > 0 && rows.every(row => !!row.DestinationColumnName);
        const allowedDestinations = new Set(destinations.map(destination => destination.toLowerCase()));
        const effectiveRequired = requiredDestinations.filter(required => allowedDestinations.has(required.toLowerCase()));
        const mappedDestinations = new Set(normalizedDestinations);
        const allRequiredDestinationsMapped = effectiveRequired
            .every(required => mappedDestinations.has(required.toLowerCase()));
        const state: ImportDataMappingState = {
            mapping: {
                SheetName: currentParsedFile.format === 'csv' ? null : currentSheetName || null,
                Mapping: contractRows
            },
            initialRow: currentHeaderRow,
            isValid,
            isSuccessful: isValid && (allSourceColumnsMapped || allRequiredDestinationsMapped),
            sourceColumnCount: rows.length,
            mappedColumnCount: contractRows.length,
            ignoredSourceColumns: rows.filter(row => !row.DestinationColumnName).map(sourceColumnLabel),
            omittedRequiredDestinations: effectiveRequired.filter(required => !mappedDestinations.has(required.toLowerCase()))
        };

        setMappingState(state);
        onChange(state);
    }, [destinations, onChange, requiredDestinations]);

    const applySheet = useCallback((currentParsedFile: ParsedImportFile, currentSheetName: string, currentHeaderRow: number) => {
        const sheet = getSheet(currentParsedFile, currentSheetName);
        const headers = sheet?.rows[currentHeaderRow - 1] ?? [];
        const rows = buildMappingRows(headers, destinations);
        const samples = (sheet?.rows.slice(currentHeaderRow) ?? [])
            .filter(row => row.some(value => value.trim() !== ''))
            .slice(0, sampleRowCount!)
            .map(row => Array.from({ length: headers.length }, (_, index) => row[index] ?? ''));

        setMappingRows(rows);
        setSampleRows(samples);
        emitChange(rows, currentParsedFile, currentSheetName, currentHeaderRow);
    }, [destinations, emitChange, sampleRowCount]);

    useEffect(() => {
        let active = true;

        void parseImportFile(file)
            .then(result => {
                if (!active) return;
                const firstSheetName = result.sheets[0]?.name ?? '';
                setParsedFile(result);
                setSheetName(firstSheetName);
                setLoading(false);
                applySheet(result, firstSheetName, initialRow!);
            })
            .catch(reason => {
                if (!active) return;
                setLoading(false);
                setError(reason instanceof Error ? `${fileErrorLabel} ${reason.message}` : fileErrorLabel);
                onChange(undefined);
            });

        return () => { active = false; };
    }, [applySheet, file, fileErrorLabel, initialRow, onChange]);

    const updateMappingRow = (rowIndex: number, destination: string | null) => {
        if (!parsedFile) return;
        const rows = mappingRows.map((row, index) => index === rowIndex
            ? { ...row, DestinationColumnName: destination }
            : row);
        setMappingRows(rows);
        emitChange(rows, parsedFile, sheetName, headerRow);
    };

    const usedDestinations = mappingRows
        .map(row => row.DestinationColumnName?.toLowerCase())
        .filter((destination): destination is string => !!destination);

    if (!destinations.length) {
        return <Alert color="yellow" icon={<IconAlertTriangle size="1rem" />}>{noDestinationsLabel}</Alert>;
    }

    if (loading) {
        return <Center py="md"><Loader size="sm" /></Center>;
    }

    if (error) {
        return <Alert color="red" icon={<IconAlertTriangle size="1rem" />}>{error}</Alert>;
    }

    if (!parsedFile || !mappingState) return null;

    return (
        <Stack spacing="sm">
            <Group grow align="end">
                {parsedFile.format !== 'csv' &&
                    <Select
                        label={worksheetLabel}
                        data={parsedFile.sheets.map(sheet => sheet.name ?? '')}
                        value={sheetName}
                        onChange={value => {
                            const nextSheetName = value ?? '';
                            setSheetName(nextSheetName);
                            applySheet(parsedFile, nextSheetName, headerRow);
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
                        applySheet(parsedFile, sheetName, nextHeaderRow);
                    }}
                />
            </Group>

            <Alert
                color={mappingState.isSuccessful ? 'green' : 'yellow'}
                icon={mappingState.isSuccessful
                    ? <IconCircleCheck size="1rem" />
                    : <IconAlertTriangle size="1rem" />}
            >
                {mappingState.isSuccessful ? mappingCorrectLabel : mappingReviewLabel}
            </Alert>

            <Accordion
                key={mappingState.isSuccessful ? 'mapping-success' : 'mapping-review'}
                multiple
                variant="separated"
                defaultValue={mappingState.isSuccessful ? [] : ['mapping']}
            >
                <Accordion.Item value="mapping">
                    <Accordion.Control>{editMappedColumnsLabel}</Accordion.Control>
                    <Accordion.Panel>
                        <ScrollArea type="auto">
                            <Table striped highlightOnHover withBorder withColumnBorders miw={520}>
                                <thead>
                                    <tr>
                                        <th>{sourceHeaderLabel}</th>
                                        <th>{sourceIndexLabel}</th>
                                        <th>{destinationLabel}</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {mappingRows.map((row, rowIndex) => (
                                        <tr key={row.SourceIndex}>
                                            <td><Text size="sm">{sourceColumnLabel(row)}</Text></td>
                                            <td><Text size="sm">{row.SourceIndex + 1}</Text></td>
                                            <td>
                                                <Select
                                                    clearable
                                                    searchable
                                                    data={destinationData.map(destination => ({
                                                        ...destination,
                                                        disabled: destination.value.toLowerCase() !== row.DestinationColumnName?.toLowerCase()
                                                            && usedDestinations.includes(destination.value.toLowerCase())
                                                    }))}
                                                    value={row.DestinationColumnName}
                                                    onChange={value => updateMappingRow(rowIndex, value)}
                                                />
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </Table>
                        </ScrollArea>
                    </Accordion.Panel>
                </Accordion.Item>
                <Accordion.Item value="sample">
                    <Accordion.Control>{viewSampleDataLabel}</Accordion.Control>
                    <Accordion.Panel>
                        {sampleRows.length === 0
                            ? <Text size="sm" color="dimmed">{noSampleDataLabel}</Text>
                            : <ScrollArea type="auto">
                                <Table withBorder withColumnBorders miw={Math.max(520, mappingRows.length * 120)}>
                                    <thead>
                                        <tr>{mappingRows.map(row => <th key={row.SourceIndex}>{sourceColumnLabel(row)}</th>)}</tr>
                                    </thead>
                                    <tbody>
                                        {sampleRows.map((row, rowIndex) => (
                                            <tr key={rowIndex}>
                                                {row.map((value, columnIndex) => <td key={columnIndex}><Text size="xs">{value}</Text></td>)}
                                            </tr>
                                        ))}
                                    </tbody>
                                </Table>
                            </ScrollArea>
                        }
                    </Accordion.Panel>
                </Accordion.Item>
            </Accordion>
        </Stack>
    );
}
