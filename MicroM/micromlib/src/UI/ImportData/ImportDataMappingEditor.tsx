import { Alert, Center, Group, Loader, NumberInput, ScrollArea, Select, Stack, Table, Text, Title, useComponentDefaultProps } from "@mantine/core";
import { IconAlertTriangle, IconCircleCheck } from "@tabler/icons-react";
import { useMemo } from "react";
import { getImportSourceColumnLabel, ImportDataMappingAPI } from "./useImportDataMapping";

export interface ImportDataMappingEditorProps {
    mappingAPI: ImportDataMappingAPI,
    destinations: readonly string[],
    worksheetLabel?: string,
    initialRowLabel?: string,
    sourceHeaderLabel?: string,
    sourceIndexLabel?: string,
    destinationLabel?: string,
    mappingCorrectLabel?: string,
    mappingReviewLabel?: string,
    editMappedColumnsLabel?: string,
    noDestinationsLabel?: string,
}

export const ImportDataMappingEditorDefaultProps: Partial<ImportDataMappingEditorProps> = {
    worksheetLabel: "Worksheet",
    initialRowLabel: "Header row",
    sourceHeaderLabel: "Source header",
    sourceIndexLabel: "Source column",
    destinationLabel: "Destination",
    mappingCorrectLabel: "The column mapping is correct.",
    mappingReviewLabel: "The column mapping should be reviewed.",
    editMappedColumnsLabel: "Edit mapped columns",
    noDestinationsLabel: "No import destinations are available.",
};

export function ImportDataMappingEditor(props: ImportDataMappingEditorProps) {
    const {
        mappingAPI, destinations, worksheetLabel, initialRowLabel, sourceHeaderLabel,
        sourceIndexLabel, destinationLabel, mappingCorrectLabel, mappingReviewLabel,
        editMappedColumnsLabel, noDestinationsLabel
    } = useComponentDefaultProps('ImportDataMappingEditor', ImportDataMappingEditorDefaultProps, props);

    const destinationData = useMemo(
        () => destinations.map(destination => ({ value: destination, label: destination })),
        [destinations]
    );

    const usedDestinations = mappingAPI.mappingRows
        .map(row => row.DestinationColumnName?.toLowerCase())
        .filter((destination): destination is string => !!destination);

    if (!destinations.length) {
        return <Alert color="yellow" icon={<IconAlertTriangle size="1rem" />}>{noDestinationsLabel}</Alert>;
    }

    if (mappingAPI.loading) {
        return <Center py="md"><Loader size="sm" /></Center>;
    }

    if (mappingAPI.error) {
        return <Alert color="red" icon={<IconAlertTriangle size="1rem" />}>{mappingAPI.error}</Alert>;
    }

    if (!mappingAPI.parsedFile || !mappingAPI.mappingState) return null;

    return (
        <Stack spacing="sm">
            <Group grow align="end">
                {mappingAPI.parsedFile.format !== 'csv' &&
                    <Select
                        label={worksheetLabel}
                        data={mappingAPI.parsedFile.sheets.map(sheet => sheet.name ?? '')}
                        value={mappingAPI.sheetName}
                        onChange={value => mappingAPI.selectSheet(value ?? '')}
                    />
                }
                <NumberInput
                    label={initialRowLabel}
                    value={mappingAPI.headerRow}
                    min={1}
                    precision={0}
                    onChange={value => mappingAPI.selectHeaderRow(value === '' ? 1 : value)}
                />
            </Group>

            <Alert
                color={mappingAPI.mappingState.isSuccessful ? 'green' : 'yellow'}
                icon={mappingAPI.mappingState.isSuccessful
                    ? <IconCircleCheck size="1rem" />
                    : <IconAlertTriangle size="1rem" />}
            >
                {mappingAPI.mappingState.isSuccessful ? mappingCorrectLabel : mappingReviewLabel}
            </Alert>

            <Title order={6}>{editMappedColumnsLabel}</Title>
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
                        {mappingAPI.mappingRows.map((row, rowIndex) => (
                            <tr key={row.SourceIndex}>
                                <td><Text size="sm">{getImportSourceColumnLabel(row)}</Text></td>
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
                                        onChange={value => mappingAPI.updateMappingRow(rowIndex, value)}
                                    />
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </Table>
            </ScrollArea>
        </Stack>
    );
}
