import { Button, Group, Stack, Text, useComponentDefaultProps } from '@mantine/core';
import { ReactNode, useEffect, useRef, useState } from 'react';

export type ConfirmLeaveStaySaveResult = 'save' | 'leave' | 'stay';

export interface ConfirmLeaveStaySaveProps {
    title?: ReactNode,
    message?: ReactNode,
    saveLabel?: ReactNode,
    leaveLabel?: ReactNode,
    stayLabel?: ReactNode,
    onResult: (result: ConfirmLeaveStaySaveResult) => void | Promise<void>,
}

export const ConfirmLeaveStaySaveDefaultProps: Partial<ConfirmLeaveStaySaveProps> = {
    title: 'Unsaved changes',
    message: 'You have unsaved changes. Do you want to save them before leaving?',
    saveLabel: 'Save and leave',
    leaveLabel: 'Leave without saving',
    stayLabel: 'Stay',
}

export function ConfirmLeaveStaySave(props: ConfirmLeaveStaySaveProps) {
    const {
        message, saveLabel, leaveLabel, stayLabel, onResult,
    } = useComponentDefaultProps('ConfirmLeaveStaySave', ConfirmLeaveStaySaveDefaultProps, props);
    const [saving, setSaving] = useState(false);
    const mountedRef = useRef(true);

    useEffect(() => {
        return () => {
            mountedRef.current = false;
        };
    }, []);

    const handleResult = async (result: ConfirmLeaveStaySaveResult) => {
        if (saving) return;
        if (result === 'save') setSaving(true);

        try {
            await Promise.resolve(onResult(result));
        }
        finally {
            if (result === 'save' && mountedRef.current) setSaving(false);
        }
    };

    return (
        <Stack spacing="md">
            <Text>{message}</Text>
            <Group position="right">
                <Button type="button" variant="default" onClick={() => void handleResult('stay')} disabled={saving}>
                    {stayLabel}
                </Button>
                <Button type="button" color="red" variant="light" onClick={() => void handleResult('leave')} disabled={saving}>
                    {leaveLabel}
                </Button>
                <Button type="button" onClick={() => void handleResult('save')} loading={saving}>
                    {saveLabel}
                </Button>
            </Group>
        </Stack>
    );
}
