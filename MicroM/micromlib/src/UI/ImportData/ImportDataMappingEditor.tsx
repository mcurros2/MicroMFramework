import { Accordion, Alert, Center, Checkbox, Group, Loader, NumberInput, ScrollArea, Select, Stack, Table, Text, useComponentDefaultProps } from "@mantine/core";
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
    omitColumnLabel?: string,
    mappingCorrectLabel?: string,
    mappingReviewLabel?: string,
    reviewDataMappingLabel?: string,
    manualDataMappingRequiredLabel?: string,
    autonumPrimaryKeysLabel?: string,
    omitAutonumPrimaryKeyLabel?: string,
    noDestinationsLabel?: string,
}

export const ImportDataMappingEditorDefaultProps: Partial<ImportDataMappingEditorProps> = {
    worksheetLabel: "Worksheet",
    initialRowLabel: "Header row",
    sourceHeaderLabel: "Source header",
    sourceIndexLabel: "Source column",
    destinationLabel: "Destination",
    omitColumnLabel: "Omit",
    mappingCorrectLabel: "The column mapping is correct.",
    mappingReviewLabel: "The column mapping should be reviewed.",
    reviewDataMappingLabel: "Review data mapping",
    manualDataMappingRequiredLabel: "Manual data mapping required",
    autonumPrimaryKeysLabel: "Autonumber primary keys",
    omitAutonumPrimaryKeyLabel: "Omit generated primary key",
    noDestinationsLabel: "No import destinations are available.",
};

export function ImportDataMappingEditor(props: ImportDataMappingEditorProps) {
    const {
        mappingAPI, destinations, worksheetLabel, initialRowLabel, sourceHeaderLabel,
        sourceIndexLabel, destinationLabel, omitColumnLabel, mappingCorrectLabel, mappingReviewLabel,
        reviewDataMappingLabel, manualDataMappingRequiredLabel, autonumPrimaryKeysLabel,
        omitAutonumPrimaryKeyLabel, noDestinationsLabel
    } = useComponentDefaultProps('ImportDataMappingEditor', ImportDataMappingEditorDefaultProps, props);

    const destinationData = useMemo(
        () => destinations.map(destination => ({ value: destination, label: destination })),
        [destinations]
    );

    const usedDestinations = mappingAPI.mappingRows
        .map(row => row.DestinationColumnName)
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

    const mappingComplete = mappingAPI.mappingState.isSuccessful;
    const mappedDestinations = new Set(usedDestinations);
    const unmappedOmittableRequiredDestinations = mappingAPI.omittableRequiredDestinations.filter(
        destination => !mappedDestinations.has(destination)
    );
    const accordionKey = `${mappingAPI.parsedFile.format}:${mappingAPI.sheetName}:${mappingAPI.headerRow}:${mappingComplete ? 'complete' : 'incomplete'}`;

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

            <Accordion
                key={accordionKey}
                defaultValue={mappingComplete ? null : 'mapping'}
                variant="separated"
            >
                <Accordion.Item value="mapping">
                    <Accordion.Control>
                        {mappingComplete ? reviewDataMappingLabel : manualDataMappingRequiredLabel}
                    </Accordion.Control>
                    <Accordion.Panel>
                        <Stack spacing="sm">
                            <ScrollArea type="auto">
                                <Table striped highlightOnHover withBorder withColumnBorders miw={620}>
                                    <thead>
                                        <tr>
                                            <th>{sourceHeaderLabel}</th>
                                            <th>{sourceIndexLabel}</th>
                                            <th>{destinationLabel}</th>
                                            <th>{omitColumnLabel}</th>
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
                                                        disabled={row.IsOmitted}
                                                        data={destinationData.map(destination => ({
                                                            ...destination,
                                                            disabled: destination.value !== row.DestinationColumnName
                                                                && usedDestinations.includes(destination.value)
                                                        }))}
                                                        value={row.DestinationColumnName}
                                                        onChange={value => mappingAPI.updateMappingRow(rowIndex, value)}
                                                    />
                                                </td>
                                                <td>
                                                    <Center>
                                                        <Checkbox
                                                            aria-label={`${omitColumnLabel} ${getImportSourceColumnLabel(row)}`}
                                                            checked={row.IsOmitted}
                                                            onChange={event => mappingAPI.setMappingRowOmitted(rowIndex, event.currentTarget.checked)}
                                                        />
                                                    </Center>
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </Table>
                            </ScrollArea>

                            {unmappedOmittableRequiredDestinations.length > 0 &&
                                <Stack spacing={4}>
                                    <Text size="sm" fw={500}>{autonumPrimaryKeysLabel}</Text>
                                    {unmappedOmittableRequiredDestinations.map(destination =>
                                        <Checkbox
                                            key={destination}
                                            label={`${omitAutonumPrimaryKeyLabel}: ${destination}`}
                                            checked={mappingAPI.omittedRequiredDestinations.some(
                                                omitted => omitted === destination
                                            )}
                                            onChange={event => mappingAPI.setRequiredDestinationOmitted(destination, event.currentTarget.checked)}
                                        />
                                    )}
                                </Stack>
                            }
                        </Stack>
                    </Accordion.Panel>
                </Accordion.Item>
            </Accordion>
        </Stack>
    );
}
