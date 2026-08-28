import { Card, Group, Stack, Text, Title, useComponentDefaultProps } from "@mantine/core";
import { ImportDataMappingState } from "../../UI/ImportData";
import { getImportFileExtension } from "../../UI/ImportData/ImportFileParser";

export interface ImportSummaryStepProps {
    file: File | null,
    mappingState?: ImportDataMappingState,
    summaryTitleLabel?: string,
    fileLabel?: string,
    formatLabel?: string,
    unknownFormatLabel?: string,
    worksheetLabel?: string,
    headerRowLabel?: string,
    mappedColumnsLabel?: string,
    ofLabel?: string,
    ignoredSourceColumnsLabel?: string,
    omittedRequiredColumnsLabel?: string,
    noneLabel?: string,
}

export const ImportSummaryStepDefaultProps: Partial<ImportSummaryStepProps> = {
    summaryTitleLabel: "Import summary",
    fileLabel: "File",
    formatLabel: "Format",
    unknownFormatLabel: "Unknown",
    worksheetLabel: "Worksheet",
    headerRowLabel: "Header row",
    mappedColumnsLabel: "Mapped columns",
    ofLabel: "of",
    ignoredSourceColumnsLabel: "Ignored source columns",
    omittedRequiredColumnsLabel: "Omitted required columns",
    noneLabel: "None",
};

function getFileFormatLabel(file: File, unknownFormatLabel?: string) {
    const extension = getImportFileExtension(file).replace('.', '').toUpperCase();
    return extension || unknownFormatLabel;
}

export function ImportSummaryStep(props: ImportSummaryStepProps) {
    const {
        file, mappingState, summaryTitleLabel, fileLabel, formatLabel, unknownFormatLabel,
        worksheetLabel, headerRowLabel, mappedColumnsLabel, ofLabel,
        ignoredSourceColumnsLabel, omittedRequiredColumnsLabel, noneLabel
    } = useComponentDefaultProps('ImportSummaryStep', ImportSummaryStepDefaultProps, props);

    if (!file || !mappingState) return null;

    return (
        <Card withBorder>
            <Stack spacing="xs">
                <Title order={5}>{summaryTitleLabel}</Title>
                <Group position="apart"><Text size="sm" fw={500}>{fileLabel}</Text><Text size="sm">{file.name}</Text></Group>
                <Group position="apart"><Text size="sm" fw={500}>{formatLabel}</Text><Text size="sm">{getFileFormatLabel(file, unknownFormatLabel)}</Text></Group>
                {mappingState.mapping.SheetName &&
                    <Group position="apart"><Text size="sm" fw={500}>{worksheetLabel}</Text><Text size="sm">{mappingState.mapping.SheetName}</Text></Group>
                }
                <Group position="apart"><Text size="sm" fw={500}>{headerRowLabel}</Text><Text size="sm">{mappingState.initialRow}</Text></Group>
                <Group position="apart"><Text size="sm" fw={500}>{mappedColumnsLabel}</Text><Text size="sm">{mappingState.mappedColumnCount} {ofLabel} {mappingState.sourceColumnCount}</Text></Group>
                <Group position="apart"><Text size="sm" fw={500}>{ignoredSourceColumnsLabel}</Text><Text size="sm">{mappingState.ignoredSourceColumns.join(', ') || noneLabel}</Text></Group>
                <Group position="apart"><Text size="sm" fw={500}>{omittedRequiredColumnsLabel}</Text><Text size="sm">{mappingState.omittedRequiredDestinations.join(', ') || noneLabel}</Text></Group>
            </Stack>
        </Card>
    );
}
