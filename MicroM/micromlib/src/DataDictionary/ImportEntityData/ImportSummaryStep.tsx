import { Box, Button, Group, List, Stack, Text, Title, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconCircleCheck, IconCircleDashed, IconCircleX } from "@tabler/icons-react";
import { OperationStatus } from "../../client";
import { ImpDataResult } from "../../client/ImpDataResult";
import { FakeProgressBar, useModal } from "../../UI/Core";
import { ImportDataMappingAPI, ImportDataMappingState } from "../../UI/ImportData";
import { ImportSampleDataStep } from "./ImportSampleDataStep";

export interface ImportSummaryStepProps {
    file: File | null,
    mappingState?: ImportDataMappingState,
    mappingAPI?: ImportDataMappingAPI,
    importStatus?: OperationStatus<ImpDataResult>,
    summaryTitleLabel?: string,
    fileLabel?: string,
    worksheetLabel?: string,
    headerRowLabel?: string,
    columnMappingCorrectLabel?: string,
    columnMappingIncorrectLabel?: string,
    viewSampleDataLabel?: string,
    sampleDataModalTitle?: string,
    submitAndImportLabel?: string,
    importingDataLabel?: string,
}

export const ImportSummaryStepDefaultProps: Partial<ImportSummaryStepProps> = {
    summaryTitleLabel: "Import summary",
    fileLabel: "File",
    worksheetLabel: "Worksheet",
    headerRowLabel: "Header row",
    columnMappingCorrectLabel: "The column mapping is correct",
    columnMappingIncorrectLabel: "Column mapping is incorrect and needs revision",
    viewSampleDataLabel: "View sample data",
    sampleDataModalTitle: "Sample data",
    submitAndImportLabel: "Ready to submit and import",
    importingDataLabel: "Importing data",
};

export function ImportSummaryStep(props: ImportSummaryStepProps) {
    const {
        file, mappingAPI, importStatus, summaryTitleLabel, fileLabel, worksheetLabel,
        headerRowLabel, columnMappingCorrectLabel, columnMappingIncorrectLabel,
        viewSampleDataLabel, sampleDataModalTitle, submitAndImportLabel, importingDataLabel
    } = useComponentDefaultProps('ImportSummaryStep', ImportSummaryStepDefaultProps, props);
    const mappingState = props.mappingState ?? mappingAPI?.mappingState;
    const theme = useMantineTheme();
    const modals = useModal();

    if (!file || !mappingState) return null;

    const mappingComplete = mappingState.isSuccessful;
    const completedIcon = <IconCircleCheck size="1rem" color={theme.colors.green[6]} />;
    const incorrectIcon = <IconCircleX size="1rem" color={theme.colors.red[6]} />;
    const submitIcon = <IconCircleDashed size="1rem" color={theme.colors[theme.primaryColor][6]} />;

    const openSampleData = () => {
        if (!mappingAPI) return;

        void modals.open({
            modalProps: { size: 'xl', title: sampleDataModalTitle },
            content: <ImportSampleDataStep mappingAPI={mappingAPI} confirmationLabel="" />
        });
    };

    return (
        <Stack spacing="sm">
            <Title order={5}>{summaryTitleLabel}</Title>
            <List spacing="xs" center styles={{ itemWrapper: { width: '100%' } }}>
                <List.Item icon={completedIcon}>
                    <Text size="sm">
                        <Text span fw={500}>{fileLabel}: </Text>{file.name}
                        {mappingState.mapping.SheetName &&
                            <><Text span>, </Text><Text span fw={500}>{worksheetLabel}: </Text>{mappingState.mapping.SheetName}</>
                        }
                        <Text span>, </Text><Text span fw={500}>{headerRowLabel}: </Text>{mappingState.initialRow}
                    </Text>
                </List.Item>

                <List.Item icon={mappingComplete ? completedIcon : incorrectIcon}>
                    {mappingComplete
                        ? <Group spacing="xs">
                            <Text size="sm">{columnMappingCorrectLabel}</Text>
                            {mappingAPI &&
                                <Button type="button" size="xs" variant="light" onClick={openSampleData}>
                                    {viewSampleDataLabel}
                                </Button>
                            }
                        </Group>
                        : <Text size="sm">{columnMappingIncorrectLabel}</Text>
                    }
                </List.Item>

                {mappingComplete &&
                    <List.Item icon={submitIcon}>
                        <Box w="100%">
                            <Text size="sm" fw={500}>
                                {importStatus?.loading ? importingDataLabel : submitAndImportLabel}
                            </Text>
                            {importStatus?.loading && <FakeProgressBar size="xs" mt={4} w="100%" />}
                        </Box>
                    </List.Item>
                }
            </List>
        </Stack>
    );
}
