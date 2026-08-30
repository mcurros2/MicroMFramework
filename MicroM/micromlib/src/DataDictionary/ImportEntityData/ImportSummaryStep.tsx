import { Box, List, Stack, Text, Title, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconCircleCheck, IconCircleDashed } from "@tabler/icons-react";
import { OperationStatus } from "../../client";
import { ImpDataResult } from "../../client/ImpDataResult";
import { FakeProgressBar } from "../../UI/Core";
import { ImportDataMappingState } from "../../UI/ImportData";
import { getImportFileExtension } from "../../UI/ImportData/ImportFileParser";

export interface ImportSummaryStepProps {
    file: File | null,
    mappingState?: ImportDataMappingState,
    importStatus?: OperationStatus<ImpDataResult>,
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
    submitAndImportLabel?: string,
    importingDataLabel?: string,
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
    submitAndImportLabel: "Submit and import",
    importingDataLabel: "Importing data",
};

function getFileFormatLabel(file: File, unknownFormatLabel?: string) {
    const extension = getImportFileExtension(file).replace('.', '').toUpperCase();
    return extension || unknownFormatLabel;
}

interface SummaryItemProps {
    label?: string,
    value?: React.ReactNode,
    icon: React.ReactNode,
}

function SummaryItem({ label, value, icon }: SummaryItemProps) {
    return (
        <List.Item icon={icon}>
            <Text size="sm"><Text span fw={500}>{label}: </Text>{value}</Text>
        </List.Item>
    );
}

export function ImportSummaryStep(props: ImportSummaryStepProps) {
    const {
        file, mappingState, importStatus, summaryTitleLabel, fileLabel, formatLabel, unknownFormatLabel,
        worksheetLabel, headerRowLabel, mappedColumnsLabel, ofLabel, ignoredSourceColumnsLabel,
        omittedRequiredColumnsLabel, noneLabel, submitAndImportLabel, importingDataLabel
    } = useComponentDefaultProps('ImportSummaryStep', ImportSummaryStepDefaultProps, props);
    const theme = useMantineTheme();

    if (!file || !mappingState) return null;

    const completedIcon = <IconCircleCheck size="1rem" color={theme.colors.green[6]} />;
    const submitIcon = <IconCircleDashed size="1rem" color={theme.colors[theme.primaryColor][6]} />;

    return (
        <Stack spacing="sm">
            <Title order={5}>{summaryTitleLabel}</Title>
            <List spacing="xs" center styles={{ itemWrapper: { width: '100%' } }}>
                <SummaryItem icon={completedIcon} label={fileLabel} value={file.name} />
                <SummaryItem icon={completedIcon} label={formatLabel} value={getFileFormatLabel(file, unknownFormatLabel)} />
                {mappingState.mapping.SheetName &&
                    <SummaryItem icon={completedIcon} label={worksheetLabel} value={mappingState.mapping.SheetName} />
                }
                <SummaryItem icon={completedIcon} label={headerRowLabel} value={mappingState.initialRow} />
                <SummaryItem
                    icon={completedIcon}
                    label={mappedColumnsLabel}
                    value={`${mappingState.mappedColumnCount} ${ofLabel} ${mappingState.sourceColumnCount}`}
                />
                <SummaryItem
                    icon={completedIcon}
                    label={ignoredSourceColumnsLabel}
                    value={mappingState.ignoredSourceColumns.join(', ') || noneLabel}
                />
                <SummaryItem
                    icon={completedIcon}
                    label={omittedRequiredColumnsLabel}
                    value={mappingState.omittedRequiredDestinations.join(', ') || noneLabel}
                />
                <List.Item icon={submitIcon}>
                    <Box w="100%">
                        <Text size="sm" fw={500}>
                            {importStatus?.loading ? importingDataLabel : submitAndImportLabel}
                        </Text>
                        {importStatus?.loading && <FakeProgressBar size="xs" mt={4} w="100%" />}
                    </Box>
                </List.Item>
            </List>
        </Stack>
    );
}
