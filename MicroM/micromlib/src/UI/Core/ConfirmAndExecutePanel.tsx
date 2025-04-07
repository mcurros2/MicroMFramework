import { Button, ButtonProps, Group, Stack, useComponentDefaultProps } from "@mantine/core";
import { IconCircleCheck, IconCircleX } from "@tabler/icons-react";
import { ReactNode, useEffect } from "react";
import { DataOperationType } from "../../client";
import { AlertError } from "./AlertError";
import { FakeProgressBar } from "./FakeProgressBar";
import { useOperationStatusCallback } from "./useOperationStatusCallback";

export interface ConfirmPanelProps {
    content: ReactNode,
    loadingContent?: ReactNode,
    operation: DataOperationType,
    onOK: () => Promise<unknown>,
    onCancel: () => Promise<void> | void,
    cancelButtonText?: string,
    okButtonText?: string,
    okButtonProps?: ButtonProps,
    cancelButtonProps?: ButtonProps,
    runOnOpen?: boolean
}

export const ConfirmAndExecutePanelDefaultProps: Partial<ConfirmPanelProps> = {
    cancelButtonText: "Cancel",
    okButtonText: "OK",
    loadingContent: <FakeProgressBar />,
    okButtonProps: { color: "red" }
}

export function ConfirmAndExecutePanel(props: ConfirmPanelProps) {
    const {
        content, onOK, onCancel, operation, okButtonText, cancelButtonText, loadingContent, okButtonProps, cancelButtonProps,
        runOnOpen
    } = useComponentDefaultProps('ConfirmAndExecutePanel', ConfirmAndExecutePanelDefaultProps, props);

    const { operationCallback, status } = useOperationStatusCallback<unknown>({
        callback: async () => await onOK(),
        operation: operation,
        deps: []
    });

    useEffect(() => {
        const runAsync = async () => {
            await operationCallback();
        }
        if (runOnOpen) {
            runAsync();
        }
    }, []);

    return (
        <Stack>
            {status.loading && loadingContent}
            {!status.loading && !status.error && content}
            {status.error &&
                <AlertError mt="sm"><>{status.error.status} {status.error.message} {status.error.statusMessage}</></AlertError>
            }
            <Group mt="xs" position="right">
                {cancelButtonText &&
                    <Button {...cancelButtonProps} variant="light" leftIcon={<IconCircleX size="1.5rem" />} onClick={async () => await Promise.resolve(onCancel())} >{cancelButtonText}</Button>
                }
                {okButtonText &&
                    <Button {...okButtonProps} onClick={async () => await operationCallback()} disabled={!!status.error} loading={status?.loading} leftIcon={<IconCircleCheck size="1.5rem" />}>{okButtonText}</Button>
                }
            </Group>
        </Stack>
    );
}