import { ScrollArea, Stack, Table, Text, useComponentDefaultProps } from "@mantine/core";
import { getImportSourceColumnLabel, ImportDataMappingAPI } from "../../UI/ImportData";

export interface ImportSampleDataStepProps {
    mappingAPI: ImportDataMappingAPI,
    confirmationLabel?: string,
    noSampleDataLabel?: string,
}

export const ImportSampleDataStepDefaultProps: Partial<ImportSampleDataStepProps> = {
    confirmationLabel: "Confirm that the data mapping matches the sample data before continuing.",
    noSampleDataLabel: "The file does not contain sample data below the selected header row.",
};

export function ImportSampleDataStep(props: ImportSampleDataStepProps) {
    const { mappingAPI, confirmationLabel, noSampleDataLabel } = useComponentDefaultProps(
        'ImportSampleDataStep',
        ImportSampleDataStepDefaultProps,
        props
    );

    return (
        <Stack spacing="sm">
            <Text size="sm">{confirmationLabel}</Text>
            {mappingAPI.sampleRows.length === 0
                ? <Text size="sm" color="dimmed">{noSampleDataLabel}</Text>
                : <ScrollArea type="auto">
                    <Table withBorder withColumnBorders miw={Math.max(520, mappingAPI.mappingRows.length * 120)}>
                        <thead>
                            <tr>
                                {mappingAPI.mappingRows.map(row =>
                                    <th key={row.SourceIndex}>{getImportSourceColumnLabel(row)}</th>
                                )}
                            </tr>
                        </thead>
                        <tbody>
                            {mappingAPI.sampleRows.map((row, rowIndex) => (
                                <tr key={rowIndex}>
                                    {row.map((value, columnIndex) =>
                                        <td key={columnIndex}><Text size="xs">{value}</Text></td>
                                    )}
                                </tr>
                            ))}
                        </tbody>
                    </Table>
                </ScrollArea>
            }
        </Stack>
    );
}
