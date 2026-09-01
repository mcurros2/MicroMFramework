import { useCallback, useEffect, useRef, useState } from "react";
import { FileImportColumnMapping, FileImportMapping } from "../../client";
import { ParsedImportFile, parseImportFile } from "./ImportFileParser";

export interface ImportDataMappingRow {
    SourceHeader: string,
    SourceIndex: number,
    DestinationColumnName: string | null,
    IsOmitted: boolean,
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
    unresolvedSourceColumns: string[],
    unmappedRequiredDestinations: string[],
}

export interface UseImportDataMappingProps {
    destinations: readonly string[],
    requiredDestinations: readonly string[],
    omittableRequiredDestinations?: readonly string[],
    initialRow?: number,
    sampleRowCount?: number,
    fileErrorLabel?: string,
}

export interface ImportDataMappingAPI {
    parsedFile?: ParsedImportFile,
    sheetName: string,
    headerRow: number,
    mappingRows: ImportDataMappingRow[],
    sampleRows: string[][],
    mappingState?: ImportDataMappingState,
    loading: boolean,
    error?: string,
    revision: number,
    omittableRequiredDestinations: readonly string[],
    omittedRequiredDestinations: readonly string[],
    loadFile: (file: File) => Promise<void>,
    reset: () => void,
    selectSheet: (sheetName: string) => void,
    selectHeaderRow: (headerRow: number) => void,
    updateMappingRow: (rowIndex: number, destination: string | null) => void,
    setMappingRowOmitted: (rowIndex: number, omitted: boolean) => void,
    setRequiredDestinationOmitted: (destination: string, omitted: boolean) => void,
}

function buildMappingRows(headers: readonly string[], destinations: readonly string[]) {
    const destinationNames = new Map(destinations.map(destination => [destination.trim().toLowerCase(), destination]));
    const usedDestinations = new Set<string>();

    return headers.map<ImportDataMappingRow>((sourceHeader, sourceIndex) => {
        const matchingDestination = destinationNames.get(sourceHeader.trim().toLowerCase());
        const normalizedDestination = matchingDestination?.toLowerCase();
        const canUseDestination = matchingDestination && normalizedDestination && !usedDestinations.has(normalizedDestination);
        if (canUseDestination) usedDestinations.add(normalizedDestination);

        return {
            SourceHeader: sourceHeader,
            SourceIndex: sourceIndex,
            DestinationColumnName: canUseDestination ? matchingDestination : null,
            IsOmitted: false
        };
    });
}

function getSheet(parsedFile: ParsedImportFile, sheetName: string) {
    return parsedFile.format === 'csv'
        ? parsedFile.sheets[0]
        : parsedFile.sheets.find(sheet => sheet.name === sheetName);
}

export function getImportSourceColumnLabel(row: ImportDataMappingRow) {
    return row.SourceHeader.trim() || `#${row.SourceIndex + 1}`;
}

export function useImportDataMapping({
    destinations,
    requiredDestinations,
    omittableRequiredDestinations = [],
    initialRow = 1,
    sampleRowCount = 10,
    fileErrorLabel = "The selected file could not be read."
}: UseImportDataMappingProps): ImportDataMappingAPI {
    const [parsedFile, setParsedFile] = useState<ParsedImportFile>();
    const [sheetName, setSheetName] = useState('');
    const [headerRow, setHeaderRow] = useState(initialRow);
    const [mappingRows, setMappingRows] = useState<ImportDataMappingRow[]>([]);
    const [sampleRows, setSampleRows] = useState<string[][]>([]);
    const [mappingState, setMappingState] = useState<ImportDataMappingState>();
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string>();
    const [revision, setRevision] = useState(0);
    const [omittedRequiredDestinations, setOmittedRequiredDestinations] = useState<string[]>([]);

    const parseToken = useRef<symbol>();

    useEffect(() => () => {
        parseToken.current = undefined;
    }, []);

    const emitChange = useCallback((
        rows: ImportDataMappingRow[],
        currentParsedFile: ParsedImportFile,
        currentSheetName: string,
        currentHeaderRow: number,
        currentOmittedRequiredDestinations: readonly string[]
    ) => {
        const contractRows = rows
            .filter(row => !!row.DestinationColumnName)
            .map<FileImportColumnMapping>(row => ({
                SourceHeader: row.SourceHeader.trim() || null,
                SourceIndex: row.SourceIndex,
                DestinationColumnName: row.DestinationColumnName!
            }));

        const mappedDestinationNames = contractRows.map(row => row.DestinationColumnName);
        const uniqueDestinations = new Set(mappedDestinationNames);

        const isValid = contractRows.length > 0 && uniqueDestinations.size === mappedDestinationNames.length;
        const allSourceColumnsResolved = rows.length > 0 && rows.every(row => !!row.DestinationColumnName || row.IsOmitted);
        const allowedDestinations = new Set(destinations);

        const effectiveRequired = requiredDestinations.filter(required => allowedDestinations.has(required));
        const effectiveOmittable = new Set(
            omittableRequiredDestinations
                .filter(destination => allowedDestinations.has(destination))
        );
        const mappedDestinations = new Set(mappedDestinationNames);
        const explicitOmittedRequired = currentOmittedRequiredDestinations.filter(destination => {
            return effectiveOmittable.has(destination) && !mappedDestinations.has(destination);
        });
        const omittedRequired = new Set(explicitOmittedRequired);
        const unmappedRequired = effectiveRequired.filter(required => {
            return !mappedDestinations.has(required) && !omittedRequired.has(required);
        });

        setMappingState({
            mapping: {
                SheetName: currentParsedFile.format === 'csv' ? null : currentSheetName || null,
                Mapping: contractRows
            },
            initialRow: currentHeaderRow,
            isValid,
            isSuccessful: isValid && allSourceColumnsResolved && unmappedRequired.length === 0,
            sourceColumnCount: rows.length,
            mappedColumnCount: contractRows.length,
            ignoredSourceColumns: rows.filter(row => row.IsOmitted).map(getImportSourceColumnLabel),
            omittedRequiredDestinations: explicitOmittedRequired,
            unresolvedSourceColumns: rows
                .filter(row => !row.DestinationColumnName && !row.IsOmitted)
                .map(getImportSourceColumnLabel),
            unmappedRequiredDestinations: unmappedRequired
        });

        setRevision(current => current + 1);
    }, [destinations, omittableRequiredDestinations, requiredDestinations]);

    const applySheet = useCallback((currentParsedFile: ParsedImportFile, currentSheetName: string, currentHeaderRow: number) => {
        const sheet = getSheet(currentParsedFile, currentSheetName);
        const headers = sheet?.rows[currentHeaderRow - 1] ?? [];
        const rows = buildMappingRows(headers, destinations);

        const samples = (sheet?.rows.slice(currentHeaderRow) ?? [])
            .filter(row => row.some(value => value.trim() !== ''))
            .slice(0, sampleRowCount)
            .map(row => Array.from({ length: headers.length }, (_, index) => row[index] ?? ''));

        setMappingRows(rows);
        setSampleRows(samples);
        setOmittedRequiredDestinations([]);

        emitChange(rows, currentParsedFile, currentSheetName, currentHeaderRow, []);
    }, [destinations, emitChange, sampleRowCount]);

    const reset = useCallback(() => {
        parseToken.current = undefined;
        setParsedFile(undefined);
        setSheetName('');
        setHeaderRow(initialRow);
        setMappingRows([]);
        setSampleRows([]);
        setMappingState(undefined);
        setLoading(false);
        setError(undefined);
        setOmittedRequiredDestinations([]);
        setRevision(current => current + 1);
    }, [initialRow]);

    const loadFile = useCallback(async (file: File) => {
        const token = Symbol("useImportDataMapping parse");
        parseToken.current = token;

        setParsedFile(undefined);
        setSheetName('');
        setHeaderRow(initialRow);
        setMappingRows([]);
        setSampleRows([]);
        setMappingState(undefined);
        setLoading(true);
        setError(undefined);
        setOmittedRequiredDestinations([]);

        setRevision(current => current + 1);

        try {
            const result = await parseImportFile(file);
            if (parseToken.current !== token) return;

            const firstSheetName = result.sheets[0]?.name ?? '';

            setParsedFile(result);
            setSheetName(firstSheetName);
            setLoading(false);
            applySheet(result, firstSheetName, initialRow);
        }
        catch (reason) {
            if (parseToken.current !== token) return;

            setLoading(false);
            setError(reason instanceof Error ? `${fileErrorLabel} ${reason.message}` : fileErrorLabel);
        }
    }, [applySheet, fileErrorLabel, initialRow]);

    const selectSheet = useCallback((nextSheetName: string) => {
        if (!parsedFile) return;
        setSheetName(nextSheetName);
        applySheet(parsedFile, nextSheetName, headerRow);
    }, [applySheet, headerRow, parsedFile]);

    const selectHeaderRow = useCallback((nextHeaderRow: number) => {
        if (!parsedFile) return;
        const normalizedHeaderRow = Math.max(1, nextHeaderRow);
        setHeaderRow(normalizedHeaderRow);
        applySheet(parsedFile, sheetName, normalizedHeaderRow);
    }, [applySheet, parsedFile, sheetName]);

    const updateMappingRow = useCallback((rowIndex: number, destination: string | null) => {
        if (!parsedFile) return;

        const rows = mappingRows.map((row, index) => index === rowIndex
            ? { ...row, DestinationColumnName: destination, IsOmitted: false }
            : row);
        const nextOmittedRequiredDestinations = destination
            ? omittedRequiredDestinations.filter(required => required !== destination)
            : omittedRequiredDestinations;

        setMappingRows(rows);
        setOmittedRequiredDestinations(nextOmittedRequiredDestinations);
        emitChange(rows, parsedFile, sheetName, headerRow, nextOmittedRequiredDestinations);
    }, [emitChange, headerRow, mappingRows, omittedRequiredDestinations, parsedFile, sheetName]);

    const setMappingRowOmitted = useCallback((rowIndex: number, omitted: boolean) => {
        if (!parsedFile) return;

        const rows = mappingRows.map((row, index) => index === rowIndex
            ? { ...row, DestinationColumnName: omitted ? null : row.DestinationColumnName, IsOmitted: omitted }
            : row);

        setMappingRows(rows);
        emitChange(rows, parsedFile, sheetName, headerRow, omittedRequiredDestinations);
    }, [emitChange, headerRow, mappingRows, omittedRequiredDestinations, parsedFile, sheetName]);

    const setRequiredDestinationOmitted = useCallback((destination: string, omitted: boolean) => {
        if (!parsedFile || !omittableRequiredDestinations.includes(destination)) return;

        const nextOmittedRequiredDestinations = omitted
            ? [...omittedRequiredDestinations.filter(required => required !== destination), destination]
            : omittedRequiredDestinations.filter(required => required !== destination);

        setOmittedRequiredDestinations(nextOmittedRequiredDestinations);
        emitChange(mappingRows, parsedFile, sheetName, headerRow, nextOmittedRequiredDestinations);
    }, [emitChange, headerRow, mappingRows, omittedRequiredDestinations, omittableRequiredDestinations, parsedFile, sheetName]);

    return {
        parsedFile,
        sheetName,
        headerRow,
        mappingRows,
        sampleRows,
        mappingState,
        loading,
        error,
        revision,
        omittableRequiredDestinations,
        omittedRequiredDestinations,
        loadFile,
        reset,
        selectSheet,
        selectHeaderRow,
        updateMappingRow,
        setMappingRowOmitted,
        setRequiredDestinationOmitted
    };
}
