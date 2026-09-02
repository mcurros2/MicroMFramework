import { Card, Group, Stack, Table, Title, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconCheck, IconX } from "@tabler/icons-react";
import { ImpDataResult } from "../../client/ImpDataResult";
import { CircleFilledIcon } from "../../UI/Core/CircleFilledIcon";

export interface ImportCompletedContentProps {
    fileName: string,
    result: ImpDataResult,
    importedFileLabel?: string,
    recordsImportedSuccessfullyLabel?: string,
    recordsNotImportedDueToErrorsLabel?: string,
    errorColumnTitle?: string,
    rowColumnTitle?: string,
}

export const ImportCompletedContentDefaultProps: Partial<ImportCompletedContentProps> = {
    importedFileLabel: "Imported file",
    recordsImportedSuccessfullyLabel: "Records imported successfully",
    recordsNotImportedDueToErrorsLabel: "Records not imported due to errors",
    errorColumnTitle: "Error",
    rowColumnTitle: "Row",
};

export function ImportCompletedContent(props: ImportCompletedContentProps) {
    const {
        fileName, result, importedFileLabel, recordsImportedSuccessfullyLabel,
        recordsNotImportedDueToErrorsLabel, errorColumnTitle, rowColumnTitle,
    } = useComponentDefaultProps('ImportCompletedContent', ImportCompletedContentDefaultProps, props);
    const theme = useMantineTheme();

    return (
        <Stack>
            <Card withBorder>
                <Stack>
                    <Title order={6}>{importedFileLabel} {fileName}</Title>
                    {result.SuccessCount > 0 &&
                        <Group spacing={0}>
                            <CircleFilledIcon icon={<IconCheck size="0.75rem" />} backColor={theme.colors.green[8]} />
                            {result.SuccessCount} {recordsImportedSuccessfullyLabel}
                        </Group>
                    }
                    {result.ErrorCount > 0 &&
                        <Group spacing={0}>
                            <CircleFilledIcon icon={<IconX size="0.75rem" />} backColor={theme.colors.red[8]} />
                            {result.ErrorCount} {recordsNotImportedDueToErrorsLabel}
                        </Group>
                    }
                    {result.ErrorCount > 0 &&
                        <Table withBorder striped width="100%">
                            <thead><tr><th>{rowColumnTitle}</th><th>{errorColumnTitle}</th></tr></thead>
                            <tbody>
                                {Object.entries(result.Errors).slice(0, 10).map(([key, value]) => (
                                    <tr key={key}><td>{key}</td><td>{value}</td></tr>
                                ))}
                            </tbody>
                        </Table>
                    }
                </Stack>
            </Card>
        </Stack>
    );
}
