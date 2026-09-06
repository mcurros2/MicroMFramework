import { Button, Group, Stack, Text } from "@mantine/core";
import { IconAlertCircle, IconAlertTriangle } from "@tabler/icons-react";
import { ReactNode, useCallback } from "react";
import { ConfirmAndExecutePanel } from "../Core/ConfirmAndExecutePanel";
import { useModal } from "../Core/ModalsManager";

export const ValidateFormLabelsDefaultProps = {
    warningTitle: 'Warning',
    errorTitle: 'Validation error',
    doYouWantToContinueLabel: 'Do you want to continue?',
    continueLabel: 'Continue',
    cancelLabel: 'Cancel',
    closeLabel: 'Close',
    unexpectedErrorLabel: 'An unexpected error occurred while validating the form.',
}

export function useValidateFormModals() {
    const modals = useModal();

    const confirmWarning = useCallback((warning: ReactNode): Promise<boolean> => {
        return new Promise<boolean>((resolve) => {
            let confirmed = false;
            void modals.open({
                modalProps: {
                    title: <Group><IconAlertTriangle size="1.5rem" /> <Text fw={700}>{ValidateFormLabelsDefaultProps.warningTitle}</Text></Group>,
                },
                content: <ConfirmAndExecutePanel
                    content={<Stack spacing="xs">{warning}<Text size="sm">{ValidateFormLabelsDefaultProps.doYouWantToContinueLabel}</Text></Stack>}
                    operation="other"
                    okButtonText={ValidateFormLabelsDefaultProps.continueLabel}
                    cancelButtonText={ValidateFormLabelsDefaultProps.cancelLabel}
                    onOK={async () => { confirmed = true; await modals.close(); }}
                    onCancel={async () => { await modals.close(); }}
                />,
                // single resolution point: fires on any close, so an external close counts as cancel
                onClosed: () => resolve(confirmed),
            });
        });
    }, [modals]);

    const showValidationError = useCallback((error: ReactNode): Promise<void> => {
        return new Promise<void>((resolve) => {
            void modals.open({
                content: <Stack>{error}<Group position="right"><Button onClick={() => void modals.close()}>{ValidateFormLabelsDefaultProps.closeLabel}</Button></Group></Stack>,
                modalProps: {
                    title: <Group spacing="xs"><IconAlertCircle size="1.25rem" color="red" /><Text fw={700}>{ValidateFormLabelsDefaultProps.errorTitle}</Text></Group>,
                    size: 'md',
                    withCloseButton: false,
                    closeOnClickOutside: false,
                    closeOnEscape: false,
                },
                onClosed: () => resolve(),
            });
        });
    }, [modals]);

    return { confirmWarning, showValidationError };
}
