import { Button, Card, Group, Text, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconCircleCheck, IconCircleX, IconInfoCircle } from "@tabler/icons-react";
import { useCallback, useRef } from "react";
import { DataGrid, DataGridProps, DataGridSelectionKeys } from "../DataGrid";
import { GridSelection } from "../Grid";

export interface LookupFormProps {
    dataGridProps: DataGridProps,
    onOK: (selectedKeys: DataGridSelectionKeys) => void,
    onCancel?: () => void,
    helpMessage?: string,
    okLabel?: string,
    cancelLabel?: string
};

//Seleccione los registros que desea y haga clic en el botón OK
export const LookupFormDefaultProps: Partial<LookupFormProps> = {
    helpMessage: "Select the records that you need and click OK",
    okLabel: "OK",
    cancelLabel: "Cancel"
}

export function LookupForm(props: LookupFormProps) {
    const { dataGridProps, onOK, onCancel, okLabel, cancelLabel, helpMessage } = useComponentDefaultProps('LookupForm', LookupFormDefaultProps, props);
    const theme = useMantineTheme();
    const selectionKeys = useRef<DataGridSelectionKeys>([]);

    const handleSelectionChanged = useCallback((selection: GridSelection, keys: DataGridSelectionKeys) => {
        selectionKeys.current = keys;
    }, []);

    const handleOK = useCallback(() => {
        onOK(selectionKeys.current);
    }, [onOK]);

    return (
        <>
            <Card shadow="sm" withBorder={theme.colorScheme === 'dark' ? false : true}>
                <Card.Section p="xs" bg={theme.colorScheme === 'dark' ? theme.colors.dark[5] : theme.colors[theme.primaryColor][3]} mb="1rem">
                    <Group sx={{ gap: "0.25rem" }}>
                        <IconInfoCircle size="1.1rem" />
                        <Text fz="xs" c="dimmed">{helpMessage}</Text>
                    </Group>
                </Card.Section>
                <DataGrid {...dataGridProps} onSelectionChanged={handleSelectionChanged} autoFocus />
            </Card>
            <Group mt="md" position="right">
                <Button variant="light" leftIcon={<IconCircleX size="1.5rem" />} onClick={() => (onCancel) ? onCancel() : null}>{cancelLabel}</Button>
                <Button onClick={handleOK} color={theme.colors.green[5]} leftIcon={<IconCircleCheck size="1.5rem" />}>{okLabel}</Button>
            </Group>
        </>
    );
}